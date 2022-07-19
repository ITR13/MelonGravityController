using ActionMenuApi.Api;
using MelonLoader;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace GravityController.Util
{
    internal class VRCIntegration
    {
        private GravityMod _mod;
        private AssetBundle _iconsAssetBundle = null;

        private MelonPreferences_Category _melon_selfCategory;
        private MelonPreferences_Entry<bool> _melon_showDebugMessages, _melon_useIcons;
        private MelonPreferences_Entry<float> _melon_increment;

        private PedalOption _gravityRadialDisplay;

        private Texture2D _addIcon, _minusIcon, _refreshIcon, _gravityIcon, _zeroIcon;

        public VRCIntegration()
        {
            _mod = GravityMod.instance;

            // Setup melonprefs:
            _melon_selfCategory = MelonPreferences.CreateCategory(ModInfo.InternalName);
            _melon_showDebugMessages = (MelonPreferences_Entry<bool>)_melon_selfCategory.CreateEntry("showDebugMessages", GravityMod.ShowDebugMessages, "Show Debug", false);
            _melon_useIcons = (MelonPreferences_Entry<bool>)_melon_selfCategory.CreateEntry("showSpecialIcons", GravityMod.ShowSpecialIcons, "Show Icon", "Shows the gravity amout visually when changed as an icon in the action menu.", false);
            _melon_increment = (MelonPreferences_Entry<float>)_melon_selfCategory.CreateEntry("Increment", GravityMod.Increment, "Increment", "Set the increment of the value that the ActionMenu Adjusts", false);

            // In case there was a previous setting, sync on start:
            UpdateFromMelonPrefs();
        }

        // Keep vars in sync with config from prefs:
        internal void UpdateFromMelonPrefs()
        {
            GravityMod.ShowDebugMessages = _melon_showDebugMessages.Value;
            GravityMod.ShowSpecialIcons = _melon_useIcons.Value;
            GravityMod.Increment = _melon_increment.Value;
        }

        // Build and execute ActionMenu
        internal void InitActionMenu()
        {
            // Load actionmenu Icon Bundle:
            var assem = Assembly.GetExecutingAssembly();
            using (var stream = assem.GetManifestResourceStream(assem.GetManifestResourceNames().Single(str => str.Contains("gravityicons.assetbundle"))))
            {
                using (var tempStream = new MemoryStream((int)stream.Length))
                {
                    stream.CopyTo(tempStream);
                    _iconsAssetBundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                    _iconsAssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }
            }

            _addIcon = _iconsAssetBundle.LoadAsset_Internal("Assets/noun_Plus.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            _addIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            _minusIcon = _iconsAssetBundle.LoadAsset_Internal("Assets/noun_Minus.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            _minusIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            _refreshIcon = _iconsAssetBundle.LoadAsset_Internal("Assets/noun_Refresh.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            _refreshIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            _gravityIcon = _iconsAssetBundle.LoadAsset_Internal("Assets/noun_Gravity.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            _gravityIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            _zeroIcon = _iconsAssetBundle.LoadAsset_Internal("Assets/noun_Off.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            _zeroIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            // Build Menu:
            VRCActionMenuPage.AddSubMenu(ActionMenuPage.Main, "Gravity", new Action(() =>
            {
                CustomSubMenu.AddButton("Reset Gravity", () =>
                {
                    _mod.ResetGravity();
                    MelonLogger.Msg("Resetting gravity to default.");
                }, _refreshIcon);
                CustomSubMenu.AddButton("Zero Gravity", () =>
                {
                    if (_mod.SetGravity(0))
                    {
                        MelonLogger.Msg("Set gravity to 0.");
                    }
                }, _zeroIcon);

                // Adding button to show the gravity amount:
                _gravityRadialDisplay = CustomSubMenu.AddButton($"Gravity: {_mod.CurrentGravity.y}", () => { }, _gravityIcon);

                CustomSubMenu.AddButton("Increase", () =>
                {
                    if (_mod.AdjustGravity(-_melon_increment.Value))
                    {
                        if (GravityMod.ShowDebugMessages) MelonLogger.Msg("Made gravity stronger.");
                    }
                }, _addIcon);
                CustomSubMenu.AddButton("Decrease", () =>
                {
                    if (_mod.AdjustGravity(_melon_increment.Value))
                    {
                        if (GravityMod.ShowDebugMessages) MelonLogger.Msg("Made gravity weaker.");
                    }
                }, _minusIcon);
            }), _gravityIcon);
        }

        internal void updateGravityAmount()
        {
            if (_gravityRadialDisplay != null)
            {
                if (GravityMod.ShowSpecialIcons)
                {
                    var whichIcon = _mod.CurrentGravity.y > _mod.BaseGravity.y ? _minusIcon :
                        _mod.CurrentGravity.y < _mod.BaseGravity.y ? _addIcon : _gravityIcon;
                    _gravityRadialDisplay.prop_Texture2D_0 = whichIcon;
                }
                _gravityRadialDisplay.prop_String_0 = $"Gravity: {_mod.CurrentGravity.y}";
            }
        }
    }
}
