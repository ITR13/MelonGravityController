using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace GravityController
{
    public class MainClass : MelonMod
    {
        private Vector3 _defaultGravity, _currentGravity;
        private List<GravityConfig> _activeConfigs = new List<GravityConfig>();

        public override void OnApplicationStart()
        {
            _defaultGravity = Physics.gravity;
            _currentGravity = _defaultGravity;
        }

        public override void OnApplicationQuit()
        {
            ConfigWatcher.Unload();
        }

        public override void OnUpdate()
        {
            CheckForGravityChange();
            UpdateConfigs();
            RunConfigs();
        }

        private void CheckForGravityChange()
        {
            var newGravity = Physics.gravity;
            if (newGravity == _currentGravity) return;
            MelonModLogger.Log(
                "Detected change from {0} to {1}. Default was: {2}",
                FormatVector3(_currentGravity),
                FormatVector3(newGravity),
                FormatVector3(_defaultGravity)
            );
            _defaultGravity = newGravity;
            _currentGravity = newGravity;
        }

        private void RunConfigs()
        {
            foreach (var gravityConfig in ConfigWatcher.GravityConfigs)
            {
                var trigger = gravityConfig.trigger;
                var hold = gravityConfig.hold;

                if (trigger == KeyCode.None) continue;
                if (
                    hold != KeyCode.None &&
                    !Input.GetKey(hold)
                ) continue;

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

                if(!Input.GetKeyDown(gravityConfig.trigger)) continue;
                RunConfig(gravityConfig, !gravityConfig.Enabled);
            }
        }

        private void UpdateConfigs()
        {
            if (!ConfigWatcher.UpdateIfDirty()) return;
            Physics.gravity = _defaultGravity;
            _currentGravity = _defaultGravity;
            _activeConfigs.Clear();
        }

        private void RunConfig(GravityConfig gravityConfig, bool enable)
        {
            gravityConfig.Enabled = enable;
            if (enable)
            {
                _activeConfigs.Remove(gravityConfig);
                _activeConfigs.Add(gravityConfig);

                MelonModLogger.Log(
                    "Setting gravity from {0} to {1}",
                    FormatVector3(_currentGravity),
                    FormatVector3(gravityConfig.gravity)
                );

                Physics.gravity = gravityConfig.gravity;
                _currentGravity = gravityConfig.gravity;
                return;
            }

            _activeConfigs.Remove(gravityConfig);
            var newGravity = _activeConfigs.Count > 0
                ? _activeConfigs[_activeConfigs.Count - 1].gravity
                : _defaultGravity;


            MelonModLogger.Log(
                "Disabling gravity {0}. Setting gravity from {1} to {2}",
                FormatVector3(gravityConfig.gravity),
                FormatVector3(_currentGravity),
                FormatVector3(newGravity)
            );

            Physics.gravity = newGravity;
            _currentGravity = newGravity;
        }

        private string FormatVector3(Vector3 vector3)
        {
            return $"({vector3.x}, {vector3.y}, {vector3.z})";
        }
    }
}
