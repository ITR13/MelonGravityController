using System.Collections.Generic;
using System.Linq;
using GravityController;
using GravityController.Config;
using GravityController.Util;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(GravityMod), ModInfo.InternalName, ModInfo.Version, ModInfo.Authors, ModInfo.DownloadLink)]
[assembly: MelonAdditionalDependencies("VRChatUtilityKit")]
[assembly: MelonOptionalDependencies("ActionMenuApi")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace GravityController
{
    public static class ModInfo
    {
        public const string
            Title = "GravityController",
            Copyright = "Copyright © 2022",
            Version = "1.1.0",
            Authors = "ITR13, lil-fluff, BLANKE",
            DownloadLink = "https://github.com/lil-fluff/MelonGravityController",
            InternalName = "VRC Gravity Mod";
    }

    public class GravityMod : MelonMod
    {
        public static GravityMod instance;

        public static bool ForceDisable;
        public static bool ShowDebugMessages;
        public static bool ShowSpecialIcons;
        public static bool WasDebugChanged = false;
        public static bool RecalculateGravity = false;
        public static float Increment = 5;

        public Vector3 BaseGravity { get; private set; }
        public Vector3 CurrentGravity { get; private set; }
        public Vector3 AdditionalGravity { get; private set; }

        private Dictionary<Vector3, List<GravityConfig>> _activeConfigs = new Dictionary<Vector3, List<GravityConfig>>();

        private static bool _haveAMenu;
        private static VRCIntegration _integrator;

        public override void OnApplicationStart()
        {
            instance = this;
            WorldCheck.Init();

            // Acquire startup settings:
            BaseGravity = Physics.gravity;
            CurrentGravity = BaseGravity;

            // Detect UIExpansionKit/ActionMenuApi:
            _haveAMenu = MelonHandler.Mods.Any(x => x.Info.Name.Equals("ActionMenuApi"));

            _integrator = new VRCIntegration();
            if (!_haveAMenu)
            {
                MelonLogger.Msg("This mod is designed to integrate with ActionMenuApi for easier access to the settings.");
            }
            else
            {
                _integrator.InitActionMenu();
            }
        }

        public override void OnApplicationQuit()
        {
            ConfigWatcher.Unload();
        }

        public override void OnUpdate()
        {
            if (WasDebugChanged != ShowDebugMessages)
            {
                MelonLogger.Msg("Debug set to " + ShowDebugMessages);
                WasDebugChanged = ShowDebugMessages;
            }
            CheckForGravityChange();
            ExecuteChanges();
        }

        // Call updates to anything changed in the MelonPrefs:
        public override void OnPreferencesSaved()
        {
            _integrator.UpdateFromMelonPrefs();
        }

        private void CheckForGravityChange()
        {
            if (Physics.gravity == CurrentGravity)
            {
                return;
            }
            RecalculateGravity = true;
            if (ShowDebugMessages)
            {
                MelonLogger.Msg("Detected change from {0} to {1}. Default was: {2}",
                    FormatVector3(CurrentGravity),
                    FormatVector3(Physics.gravity),
                    FormatVector3(BaseGravity)
                );
            }
            CurrentGravity = Physics.gravity;
            BaseGravity = Physics.gravity;
        }

        private void ExecuteChanges()
        {

            UpdateConfigs();
            RunConfigs();

            if (!RecalculateGravity) return;
            RecalculateGravity = false;
            if (ForceDisable)
            {
                if (Physics.gravity != BaseGravity)
                {
                    if (ShowDebugMessages)
                    {
                        MelonLogger.Msg($"Risky functions not allowed, resetting gravity to {FormatVector3(BaseGravity)}");
                    }
                    Physics.gravity = BaseGravity;
                }
                CurrentGravity = BaseGravity;
                return;
            }


            var newGravity = _activeConfigs.TryGetValue(BaseGravity, out var currentConfigs)
                ? currentConfigs.Count > 0
                    ? currentConfigs[currentConfigs.Count - 1].gravity
                    : BaseGravity
                : BaseGravity;

            newGravity += AdditionalGravity;

            if (ShowDebugMessages)
            {
                MelonLogger.Msg("Setting gravity from {0} to {1}",
                    FormatVector3(CurrentGravity),
                    FormatVector3(newGravity)
                );
            }

            CurrentGravity = newGravity;
            Physics.gravity = newGravity;
            _integrator.updateGravityAmount();
        }

        // NEW in 1.0.6 - Mainly used as utility functions for VRC/ActionMenu Integration.
        #region Direct Methods
        // Reset all changes to _defaultGravity. This is always available and DOES NOT RESPECT RISKY CHECK.
        // I chose to do this because being able to reset your gravity to Unity default (or the world's default, really)
        // is something I consider to be a critical failsafe.
        internal void ResetGravity()
        {
            _activeConfigs.Clear();
            AdditionalGravity = new Vector3(0, 0, 0);
            RecalculateGravity = true;
        }

        // Adjust gravity by amount. Negitive numbers increase gravity strength, positive decreases.
        // This does respect Risky Check(s).
        // TODO: Support multidirectional gravity later... maybe.
        internal bool AdjustGravity(float amt)
        {
            if (ForceDisable) return false;
            AdditionalGravity += new Vector3(0, amt, 0);
            RecalculateGravity = true;
            return true;
        }

        internal bool SetGravity(float amt)
        {
            if (ForceDisable) return false;
            AdditionalGravity = new Vector3(0, amt, 0) - BaseGravity;
            _activeConfigs.Clear();
            RecalculateGravity = true;
            return true;
        }
        #endregion

        private bool IsEnabled(GravityConfig gravityConfig)
        {
            return _activeConfigs.TryGetValue(BaseGravity, out var currentConfigs) &&
                currentConfigs.Contains(gravityConfig);
        }

        // Checking pressed keys against configured input - default method from ITR13
        private void RunConfigs()
        {
            foreach (var gravityConfig in ConfigWatcher.GravityConfigs)
            {
                var trigger = gravityConfig.trigger;
                var hold = gravityConfig.hold;

                if (trigger == KeyCode.None) continue;
                if (hold != KeyCode.None && !Input.GetKey(hold)) continue;

                if (gravityConfig.holdToActivate)
                {
                    if (Input.GetKeyDown(gravityConfig.trigger))
                    {
                        RunConfig(gravityConfig, true);
                    }
                    if (Input.GetKeyUp(gravityConfig.trigger))
                    {
                        RunConfig(gravityConfig, false);
                    }
                    continue;
                }

                if (!Input.GetKeyDown(gravityConfig.trigger)) continue;
                RunConfig(gravityConfig, !IsEnabled(gravityConfig));
            }
        }

        // Checking for 'dirty' filesystem changes, and hotloading them.
        // This WILL reset any user gravity changes back to _defaultGravity (captured at world join).
        private void UpdateConfigs()
        {
            if (!ConfigWatcher.UpdateIfDirty()) return;
            _activeConfigs.Clear();
            RecalculateGravity = true;
        }

        // Only runs if the keybind is on a toggle.
        private void RunConfig(GravityConfig gravityConfig, bool enable)
        {
            RecalculateGravity = true;
            if (!_activeConfigs.TryGetValue(BaseGravity, out var currentConfigs))
            {
                currentConfigs = new List<GravityConfig>();
                _activeConfigs.Add(BaseGravity, currentConfigs);
            }

            if (enable)
            {
                if (ShowDebugMessages)
                {
                    MelonLogger.Msg("Enabling gravity preset {0}.", FormatVector3(gravityConfig.gravity));
                }

                currentConfigs.Remove(gravityConfig);
                currentConfigs.Add(gravityConfig);
                return;
            }

            currentConfigs.Remove(gravityConfig);

            if (ShowDebugMessages)
            {
                MelonLogger.Msg("Disabling gravity preset {0}.", FormatVector3(gravityConfig.gravity));
            }
        }

        private string FormatVector3(Vector3 vector3)
        {
            return $"({vector3.x}, {vector3.y}, {vector3.z})";
        }

        public enum chgDir
        {
            INCREASE,
            DECREASE
        }
    }
}
