using ActionMenuApi.Api;
using MelonLoader;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace GravityController.Util
{
    internal class VRCIntegration {

        private static AssetBundle iconsAssetBundle = null;
        private GravityMod mod;
        private MelonPreferences_Category _melon_selfCategory;
        internal MelonPreferences_Entry<bool> _melon_showDebugMessages, _melon_useIcons;
        internal MelonPreferences_Entry<float> _melon_increment;

        internal PedalOption gravityRadialDisplay;

        private Texture2D addIcon, minusIcon, refreshIcon, gravityIcon, zeroIcon;

        public VRCIntegration() {
            mod = GravityMod.instance;

            // Setup melonprefs:
            _melon_selfCategory = MelonPreferences.CreateCategory(ModInfo.InternalName);
            _melon_showDebugMessages = (MelonPreferences_Entry<bool>) _melon_selfCategory.CreateEntry("showDebugMessages",GravityMod.ShowDebugMessages,"Show Debug",false);
            _melon_useIcons = (MelonPreferences_Entry<bool>)_melon_selfCategory.CreateEntry("showSpecialIcons", GravityMod.ShowSpecialIcons, "Show Icon", "Shows the gravity amout visually when changed as an icon in the action menu.", false);
            _melon_increment = (MelonPreferences_Entry<float>)_melon_selfCategory.CreateEntry("Increment", GravityMod.Increment, "Increment", "Set the increment of the value that the ActionMenu Adjusts", false);

            // In case there was a previous setting, sync on start:
            UpdateFromMelonPrefs();
        }

        // Keep vars in sync with config from prefs:
        internal void UpdateFromMelonPrefs() {
            GravityMod.ShowDebugMessages = _melon_showDebugMessages.Value;
            GravityMod.ShowSpecialIcons = _melon_useIcons.Value;
            GravityMod.Increment = _melon_increment.Value;
        }

        // Build and execute ActionMenu
        internal void InitActionMenu() {
            // Load actionmenu Icon Bundle:
            var assem = Assembly.GetExecutingAssembly();
            using (var stream = assem.GetManifestResourceStream(assem.GetManifestResourceNames().Single(str => str.Contains("gravityicons.assetbundle")))) {
                using (var tempStream = new MemoryStream((int)stream.Length)) {
                    stream.CopyTo(tempStream);
                    iconsAssetBundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                    iconsAssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }
            }

            addIcon = iconsAssetBundle.LoadAsset_Internal("Assets/noun_Plus.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            addIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            minusIcon = iconsAssetBundle.LoadAsset_Internal("Assets/noun_Minus.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            minusIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            refreshIcon = iconsAssetBundle.LoadAsset_Internal("Assets/noun_Refresh.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            refreshIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            gravityIcon = iconsAssetBundle.LoadAsset_Internal("Assets/noun_Gravity.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            gravityIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            zeroIcon = iconsAssetBundle.LoadAsset_Internal("Assets/noun_Off.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            zeroIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            // Build Menu:
            VRCActionMenuPage.AddSubMenu(ActionMenuPage.Main,"Gravity",new Action(() => {
                CustomSubMenu.AddButton("Reset Gravity", () => {
                    mod.ResetGravity();
                    MelonLogger.Msg("Resetting gravity to default.");
                }, refreshIcon);
                CustomSubMenu.AddButton("Zero Gravity", () => {
                    if (mod.SetGravity(0)) {
                        MelonLogger.Msg("Set gravity to 0.");
                    }
                }, zeroIcon);

                // Adding button to show the gravity amount:
                gravityRadialDisplay = CustomSubMenu.AddButton($"Gravity: {mod.get_currentGravity.y}", () => { }, gravityIcon);

                CustomSubMenu.AddButton("Increase", () => {
                    if (mod.AdjustGravity(-_melon_increment.Value)) {
                        if (GravityMod.ShowDebugMessages) MelonLogger.Msg("Made gravity stronger.");
                    }
                }, addIcon);
                CustomSubMenu.AddButton("Decrease", () => {
                    if (mod.AdjustGravity(_melon_increment.Value)) {
                        if (GravityMod.ShowDebugMessages) MelonLogger.Msg("Made gravity weaker.");
                    }
                }, minusIcon);
            }), gravityIcon);
        }

        internal void updateGravityAmount() {
            if (gravityRadialDisplay != null) {
                if (GravityMod.ShowSpecialIcons) {
                    var whichIcon = mod.get_currentGravity.y > mod.get_defaultGravity.y ? minusIcon :
                        mod.get_currentGravity.y < mod.get_defaultGravity.y ? addIcon : gravityIcon;
                    gravityRadialDisplay.prop_Texture2D_0 = whichIcon;
                }
                gravityRadialDisplay.prop_String_0 = $"Gravity: {mod.get_currentGravity.y}";
            }
        }

        private Texture2D textureFromPng(Image img) {
            Texture2D tex = new Texture2D(img.Width, img.Height);
            using (var ms = new MemoryStream()) {
                try {
                    img.Save(ms, img.RawFormat);
                    var result = ImageConversion.LoadImage(tex, ms.ToArray());
                    if (result) return tex;
                }
                catch {
                    MelonLogger.Error("Could not convert image asset.");
                }
            }
            return null;
        }
    }
}
