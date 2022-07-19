using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GravityController;
using GravityController.Config;
using GravityController.Util;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;

[assembly: MelonInfo(typeof(GravityMod),ModInfo.InternalName,ModInfo.Version,ModInfo.Authors)]
[assembly: MelonOptionalDependencies("UIExpansionKit", "ActionMenuApi")]
[assembly: MelonGame("VRChat","VRChat")]

namespace GravityController {
    public static class ModInfo {
        public const string
            Title = "GravityController",
            Copyright = "Copyright © 2022",
            Version = "1.0.8",
            Authors = "ITR13, lil-fluff",
            DownloadLink = "https://github.com/lil-fluff/MelonGravityController",
            InternalName = "VRC Gravity Mod";

        public static readonly string[] OptionalDependencies = { "UIExpansionKit" };
    }

    public class GravityMod : MelonMod {
        public static GravityMod instance;

        public static bool ForceDisable;
        public static bool ShowDebugMessages;
        public static bool ShowSpecialIcons;
        public static bool wasDebugChanged = false;
        public static bool wasGravityReset = false;
        public static float Increment = 5;

        private Vector3 _baseGravity, _defaultGravity, _currentGravity;
        private List<GravityConfig> _activeConfigs = new List<GravityConfig>();

        private static bool haveUIKit, haveAMenu;
        private static VRCIntegration integrator;

        public override void OnApplicationStart() {
            instance = this;

            // Acquire startup settings:
            _baseGravity = Physics.gravity;
            _defaultGravity = _baseGravity;
            _currentGravity = _baseGravity;

            // Detect UIExpansionKit/ActionMenuApi:
            haveUIKit = MelonHandler.Mods.Any(x => x.Info.Name.Equals("UI Expansion Kit"));
            haveAMenu = MelonHandler.Mods.Any(x => x.Info.Name.Equals("ActionMenuApi"));

            // Warn about enhanced utility:
            var warnMsg = "This mod is designed to integrate with";
            if (!haveUIKit && !haveAMenu) 
                MelonLogger.Warning($"{warnMsg} UIExpansionKit and ActionMenuAPI for a better user experience.");
            else if (!haveUIKit)
                MelonLogger.Msg($"{warnMsg} UIExpansionKit for a better configuration experience.");
            else if (!haveAMenu)
                MelonLogger.Msg($"{warnMsg} ActionMenuApi for easier access to the settings.");

            // Integrate with ActionMenuApi, UIKit, MelonPreferences:
            integrator = new VRCIntegration();
        }

        public override void OnSceneWasLoaded(int buildIndex,string sceneName) {
            // Do stuff on world join. Why was this a fall-through switch statement?
            // Original thanks to Arion-Kun https://github.com/Arion-Kun/PostProcessing/blob/f8ffd3bbedf67ddbf3b6fee56c7368ae4fe47a80/Start.cs#L49
            if (buildIndex == -1) MelonCoroutines.Start(WaitForInWorld());
        }

        private IEnumerator WaitForInWorld() {
            while (!IsInWorld()) {
                yield return null;
            }
            yield return new WaitForEndOfFrame();

            // Reset the flag if gravity was reset on world join:
            wasGravityReset = false;

            // This fixes the issues with Emm Check failing to load...
            MelonCoroutines.Start(WorldCheck.CheckWorld());

            // Force a check against the filesystem on world join because for some reason
            // calling SetGravity() was triggering a config update?
            UpdateConfigs();

            // Try to inject ActionMenu stuff:
            if (haveAMenu) integrator.InitActionMenu();
        }

        public override void OnApplicationQuit() {
            ConfigWatcher.Unload();
        }

        public override void OnUpdate() {
            if (wasDebugChanged != ShowDebugMessages) {
                MelonLogger.Msg("Debug set to " + ShowDebugMessages);
                wasDebugChanged = ShowDebugMessages;
            }
            CheckForGravityChange();
            ExecuteChanges();
        }

        // Call updates to anything changed in the MelonPrefs:
        public override void OnPreferencesSaved() {
            integrator.UpdateFromMelonPrefs();
        }

        // New option: Allow users to opt-in to debug messages in console and logs via MelonPreferences:
        private void CheckForGravityChange() {
            if (Physics.gravity != _currentGravity) {
                if (!ForceDisable) {
                    _currentGravity = Physics.gravity;
                    integrator.updateGravityAmount();
                }
                if (ShowDebugMessages) {
                    MelonLogger.Msg("Detected change from {0} to {1}. Default is: {2}",
                        FormatVector3(_currentGravity),
                        FormatVector3(Physics.gravity),
                        FormatVector3(_defaultGravity)
                    );
                }
            }
        }

        // Will run after logging any other gravity changes to console/log.
        private void ExecuteChanges() {
            // Automatically fixes gravity when joining fucky worlds. 
            // Need to manually set this if you /want/ to go to zero gravity.
            if (!wasGravityReset && Physics.gravity != _baseGravity) {
                Physics.gravity = _baseGravity;
                wasGravityReset = true;
            }
            if (ForceDisable) return;
            UpdateConfigs();
            RunConfigs();
        }

        // NEW in 1.0.6 - Mainly used as utility functions for VRC/ActionMenu Integration.
        #region Direct Methods
        // Reset all changes to _defaultGravity. This is always available and DOES NOT RESPECT RISKY CHECK.
        // I chose to do this because being able to reset your gravity to Unity default (or the world's default, really)
        // is something I consider to be a critical failsafe.
        internal void ResetGravity() {
            Physics.gravity = _defaultGravity;
            _currentGravity = _defaultGravity;
            integrator.updateGravityAmount();
        }

        // Adjust gravity by amount. Negitive numbers increase gravity strength, positive decreases.
        // This does respect Risky Check(s).
        // TODO: Support multidirectional gravity later... maybe.
        internal bool AdjustGravity(float amt) {
            if (ForceDisable) return false;
            
            // Workaround fix for our new detection of worlds fucking with our gravity setting.
            if (!wasGravityReset) wasGravityReset = true;
            Physics.gravity += new Vector3(0, amt, 0);
            return true;
        }

        internal bool SetGravity(float amt) {
            if (ForceDisable) return false;

            // Workaround fix for our new detection of worlds fucking with our gravity setting.
            if (!wasGravityReset) wasGravityReset = true;
            Physics.gravity = new Vector3(0, amt, 0);
            return true;
        }
        #endregion

        // Checking pressed keys against configured input - default method from ITR13
        private void RunConfigs() {
            foreach (var gravityConfig in ConfigWatcher.GravityConfigs) {
                var trigger = gravityConfig.trigger;
                var hold = gravityConfig.hold;

                if (trigger == KeyCode.None) continue;
                if (hold != KeyCode.None && !Input.GetKey(hold)) continue;

                if (gravityConfig.holdToActivate) {
                    if (Input.GetKeyDown(gravityConfig.trigger)) {
                        RunConfig(gravityConfig,true);
                    }
                    if (Input.GetKeyUp(gravityConfig.trigger)) {
                        RunConfig(gravityConfig,false);
                    }
                    continue;
                }

                if (!Input.GetKeyDown(gravityConfig.trigger)) continue;
                RunConfig(gravityConfig,!gravityConfig.Enabled);
            }
        }

        // Checking for 'dirty' filesystem changes, and hotloading them.
        // This WILL reset any user gravity changes back to _defaultGravity (captured at world join).
        private void UpdateConfigs() {
            if (!ConfigWatcher.UpdateIfDirty()) return;
            Physics.gravity = _defaultGravity;
            _currentGravity = _defaultGravity;
            _activeConfigs.Clear();
        }

        // Only runs if the keybind is on a toggle.
        private void RunConfig(GravityConfig gravityConfig, bool enable) {
            gravityConfig.Enabled = enable;
            if (enable) {
                _activeConfigs.Remove(gravityConfig);
                _activeConfigs.Add(gravityConfig);

                if (ShowDebugMessages) {
                    MelonLogger.Msg("Setting gravity from {0} to {1}",
                        FormatVector3(_currentGravity),
                        FormatVector3(gravityConfig.gravity));
                }

                Physics.gravity = gravityConfig.gravity;
                _currentGravity = gravityConfig.gravity;
                return;
            }

            _activeConfigs.Remove(gravityConfig);
            var newGravity = _activeConfigs.Count > 0 ? _activeConfigs[_activeConfigs.Count - 1].gravity : _defaultGravity;

            if (ShowDebugMessages) {
                MelonLogger.Msg("Disabling gravity preset {0}.", FormatVector3(gravityConfig.gravity));
                MelonLogger.Msg("Setting gravity from {0} to {1}",
                    FormatVector3(_currentGravity),
                    FormatVector3(newGravity)
                );
            }

            Physics.gravity = newGravity;
            _currentGravity = newGravity;
        }

        internal Vector3 get_baseGravity => _baseGravity;
        internal Vector3 get_defaultGravity => _defaultGravity;
        internal Vector3 get_currentGravity => _currentGravity;
        internal void set_currentGravity(Vector3 targetGravity) => _currentGravity = targetGravity;

        private string FormatVector3(Vector3 vector3) {
            return $"({vector3.x}, {vector3.y}, {vector3.z})";
        }

        // Stuff from my private mod, but its useful so sure, make it public i guess.
        public static bool IsInWorld() {
            return RoomManager.field_Internal_Static_ApiWorld_0 != null ||
                RoomManager.field_Internal_Static_ApiWorldInstance_0 != null;
        }

        public enum chgDir {
            INCREASE,
            DECREASE
        }
    }
}
