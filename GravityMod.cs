using System.Collections.Generic;
#if VRCHAT
using System.Linq;
#endif
using GravityController;
using GravityController.Config;
using GravityController.Util;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(GravityMod), ModInfo.InternalName, ModInfo.Version, ModInfo.Authors, ModInfo.DownloadLink)]
#if VRCHAT
[assembly: MelonAdditionalDependencies("VRChatUtilityKit")]
[assembly: MelonOptionalDependencies("ActionMenuApi")]
#endif

#if VRCHAT
[assembly: MelonGame("VRChat", "VRChat")]
#else
[assembly: MelonGame("*", "*")]
#endif


namespace GravityController
{
    public static class ModInfo
    {
        public const string
#if VRCHAT
            Title = "VRC Gravity Controller",
            InternalName = "VrcGravityController",
#else
            Title = "Gravity Controller",
            InternalName = "GravityController",
#endif
            Copyright = "Copyright © 2022",
            Version = "1.1.0",
            Authors = "ITR13, lil-fluff, BLANKE",
            DownloadLink = "https://github.com/ITR13/MelonGravityController";
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
        private Dictionary<Vector3, Stack<Vector3>> _brainFries = new Dictionary<Vector3, Stack<Vector3>>();

#if VRCHAT
        private static bool _haveAMenu;
        private static VRCIntegration _integrator;
#endif

        public override void OnApplicationStart()
        {
            instance = this;
            MelonConfig.EnsureInit();
#if VRCHAT
            WorldCheck.Init();
#endif

            // Acquire startup settings:
            BaseGravity = Physics.gravity;
            CurrentGravity = BaseGravity;

#if VRCHAT
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
#endif
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

#if VRCHAT
        // Call updates to anything changed in the MelonPrefs:
        public override void OnPreferencesSaved()
        {
            _integrator.UpdateFromMelonPrefs();
        }
#endif

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
            if (_brainFries.TryGetValue(CurrentGravity, out var fries) && fries.Count > 0)
            {
                CurrentGravity = fries.Pop();
                if (ShowDebugMessages)
                {
                    MelonLogger.Msg($"Safeguard detected brain-fry, setting new default to {FormatVector3(CurrentGravity)} instead!");
                }
            }

            if (_activeConfigs.TryGetValue(CurrentGravity, out var currentConfigs) && currentConfigs.Count > 0)
            {
                for (var i = currentConfigs.Count - 1; i >= 0; i--)
                {
                    if (!currentConfigs[i].holdToActivate) continue;
                    if (Input.GetKeyDown(currentConfigs[i].trigger)) continue;
                    SetConfig(currentConfigs[i], false);
                }
            }

            BaseGravity = CurrentGravity;
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

            // CurrentGravity will always be the same as Physics.gravity here


            var newGravity = _activeConfigs.TryGetValue(BaseGravity, out var currentConfigs)
                ? currentConfigs.Count > 0
                    ? currentConfigs[currentConfigs.Count - 1].gravity
                    : BaseGravity
                : BaseGravity;

            newGravity += AdditionalGravity;

            if (newGravity != CurrentGravity)
            {
                if (ShowDebugMessages)
                {
                    MelonLogger.Msg("Setting gravity from {0} to {1}",
                        FormatVector3(CurrentGravity),
                        FormatVector3(newGravity)
                    );
                }

                Vector3 fry = CurrentGravity;
                if (_brainFries.TryGetValue(CurrentGravity, out var fries) && fries.Count > 0)
                {
                    fry = fries.Pop();
                }

                if (!_brainFries.TryGetValue(newGravity, out var newFries))
                {
                    newFries = new Stack<Vector3>();
                    _brainFries.Add(newGravity, newFries);
                }
                newFries.Push(fry);

                CurrentGravity = newGravity;
                Physics.gravity = newGravity;
            }

#if VRCHAT
            _integrator.updateGravityAmount();
#endif
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
            _activeConfigs[BaseGravity].Clear();
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
                var isHolding = hold == KeyCode.None || Input.GetKey(hold);

                if (gravityConfig.holdToActivate)
                {
                    if (Input.GetKeyUp(gravityConfig.trigger))
                    {
                        SetConfig(gravityConfig, false);
                    }

                    if (isHolding && Input.GetKeyDown(gravityConfig.trigger))
                    {
                        SetConfig(gravityConfig, true);
                    }
                    continue;
                }

                if (!isHolding || !Input.GetKeyDown(gravityConfig.trigger)) continue;
                SetConfig(gravityConfig, !IsEnabled(gravityConfig));
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

        private void SetConfig(GravityConfig gravityConfig, bool enable)
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

            if (currentConfigs.Remove(gravityConfig) && ShowDebugMessages)
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
