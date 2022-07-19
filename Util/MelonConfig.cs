using MelonLoader;

namespace GravityController.Util
{
    public static class MelonConfig
    {
        public static MelonPreferences_Category _melon_selfCategory;
        public static MelonPreferences_Entry<bool> _melon_showDebugMessages;

        static MelonConfig()
        {
            _melon_selfCategory = MelonPreferences.CreateCategory(ModInfo.InternalName);
            _melon_showDebugMessages = (MelonPreferences_Entry<bool>)_melon_selfCategory.CreateEntry(
                "showDebugMessages", 
                GravityMod.ShowDebugMessages, 
                "Show Debug",
                false
            );
        }

        public static void EnsureInit()
        {

        }
    }
}
