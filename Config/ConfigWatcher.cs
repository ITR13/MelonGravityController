using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.TinyJSON;
using UnityEngine;

namespace GravityController.Config
{
    static class ConfigWatcher
    {
        private const string FileName = "GravityConfig.json";
        private static readonly string FileDirectory = Path.Combine(Environment.CurrentDirectory, "UserData");
        private static readonly string FullPath = Path.Combine(FileDirectory, FileName);

        public static List<GravityConfig> GravityConfigs = new List<GravityConfig>();

        private static readonly FileSystemWatcher FileSystemWatcher;
        private static bool _dirty = false;

        static ConfigWatcher()
        {
            FileSystemWatcher = new FileSystemWatcher(FileDirectory, FileName)
            {
                NotifyFilter = (NotifyFilters)((1 << 9) - 1),
                EnableRaisingEvents = true
            };
            FileSystemWatcher.Changed += (_, __) => _dirty = true;
            FileSystemWatcher.Created += (_, __) => _dirty = true;
            FileSystemWatcher.Renamed += (_, __) => _dirty = true;
            FileSystemWatcher.Deleted += (_, __) => _dirty = true;
            _dirty = true;
        }

        public static void Unload()
        {
            FileSystemWatcher.EnableRaisingEvents = false;
            _dirty = false;
        }

        // Update internal cache of configs at runtime if anything is changed.
        public static bool UpdateIfDirty()
        {
            if (!_dirty) return false;
            _dirty = false;

            if (!File.Exists(FullPath))
            {
                MelonLogger.Msg($"Creating default config file at \"{FullPath}\"");
                var sampleConfig = new List<GravityConfig> {
                    new GravityConfig(),
                    new GravityConfig{
                        gravity = new SerializedVector3(0, 9.81f, 0),
                        trigger = KeyCode.O,
                        holdToActivate = true,
                    },
                };

                var json = JSON.Dump(
                    sampleConfig,
                    EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints
                );
                File.WriteAllText(FullPath, json);
            }

            try
            {
                var json = File.ReadAllText(FullPath);
                JSON.MakeInto(JSON.Load(json), out List<GravityConfig> LoadedGravityConfigs);
                if (LoadedGravityConfigs != null) GravityConfigs = LoadedGravityConfigs;
                MelonLogger.Msg("Updated gravity config.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("Failed to update config!\n" + e.Message);
            }
            return true;
        }
    }
}
