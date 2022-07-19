using MelonLoader;
using VRChatUtilityKit.Utilities;

namespace GravityController.Util
{
    // Mostly stolen from https://github.com/loukylor/VRC-Mods/blob/main/TriggerESP/TriggerESPMod.cs#L82-L89
    internal static class WorldCheck
    {
        private static MelonPreferences_Entry<bool> _resetGravityOnWorldChanged;

        public static void Init()
        {
            _resetGravityOnWorldChanged = MelonConfig._melon_selfCategory.CreateEntry(
                "resetOnWorldChanged", 
                true, 
                "Auto-Reset",
                "Resets the gravity whenever the world changes"
            );

            GravityMod.ForceDisable = true;
            NetworkEvents.OnRoomLeft += () =>
            {
                GravityMod.ForceDisable = true;
                GravityMod.RecalculateGravity = true;

                if (_resetGravityOnWorldChanged.Value) GravityMod.instance.ResetGravity();
            };
            VRCUtils.OnEmmWorldCheckCompleted += areRiskyFuncsAllowed =>
            {
                GravityMod.ForceDisable = !areRiskyFuncsAllowed;
                GravityMod.RecalculateGravity = true;

                if (_resetGravityOnWorldChanged.Value) GravityMod.instance.ResetGravity();
            };
        }
    }
}
