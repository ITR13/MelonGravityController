using VRChatUtilityKit.Utilities;

namespace GravityController.Util
{
    // Mostly stolen from https://github.com/loukylor/VRC-Mods/blob/main/TriggerESP/TriggerESPMod.cs#L82-L89
    internal static class WorldCheck
    {
        public static void Init()
        {
            GravityMod.ForceDisable = true;
            NetworkEvents.OnRoomLeft += () =>
            {
                GravityMod.ForceDisable = true;
                GravityMod.RecalculateGravity = true;
            };
            VRCUtils.OnEmmWorldCheckCompleted += areRiskyFuncsAllowed =>
            {
                GravityMod.ForceDisable = !areRiskyFuncsAllowed;
                GravityMod.RecalculateGravity = true;
            };
        }
    }
}
