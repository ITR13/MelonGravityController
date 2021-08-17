using System;
using System.Collections;
using Il2CppSystem.Collections.Generic;

using MelonLoader;
using UnityEngine;

using VRC.Core;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace GravityController
{
    // Mostly stolen from https://github.com/Psychloor/PlayerRotater/blob/0b30e04cf85fdab769f6e0afc020e6d9bc9900ac/PlayerRotater/Utilities.cs#L76
    class WorldCheck
    {

        #region ModPatch
        // Also stolen from player rotator

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //private delegate void FadeTo(IntPtr instancePtr, IntPtr fadeNamePtr, float fade, IntPtr actionPtr, IntPtr stackPtr);

        //private static FadeTo origFadeTo;
        //private static void FadeToPatch(IntPtr instancePtr, IntPtr fadeNamePtr, float fade, IntPtr actionPtr, IntPtr stackPtr)
        //{
        //    if (instancePtr == IntPtr.Zero) return;
        //    origFadeTo(instancePtr, fadeNamePtr, fade, actionPtr, stackPtr);

        //    if (!IL2CPP.Il2CppStringToManaged(fadeNamePtr).Equals("BlackFade", StringComparison.Ordinal)
        //        || !fade.Equals(0f)
        //        || RoomManager.field_Internal_Static_ApiWorldInstance_0 == null) return;

        //    MelonCoroutines.Start(WorldCheck.CheckWorld());
        //}

        //internal static bool PatchMethods()
        //{
        //    try
        //    {
        //        // Faded to and joined and initialized room
        //        MethodInfo fadeMethod = typeof(VRCUiManager).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).First(
        //            m => m.Name.StartsWith("Method_Public_Void_String_Single_Action_")
        //                 && m.Name.IndexOf("PDM", StringComparison.OrdinalIgnoreCase) == -1
        //                 && m.GetParameters().Length == 3);
        //        origFadeTo = Patch<FadeTo>(fadeMethod, GetDetour(nameof(FadeToPatch)));
        //    }
        //    catch (Exception e)
        //    {
        //        MelonLogger.Error("Failed to patch FadeTo\n" + e.Message);
        //        return false;
        //    }

        //    return true;
        //}
 
        //private static unsafe TDelegate Patch<TDelegate>(MethodBase originalMethod, IntPtr patchDetour)
        //{
        //    IntPtr original = *(IntPtr*)UnhollowerSupport.MethodBaseToIl2CppMethodInfoPointer(originalMethod);
        //    MelonUtils.NativeHookAttach((IntPtr)(&original), patchDetour);
        //    return Marshal.GetDelegateForFunctionPointer<TDelegate>(original);
        //}

        //private static IntPtr GetDetour(string name)
        //{
        //    return typeof(WorldCheck).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).MethodHandle.GetFunctionPointer();
        //}

        #endregion

        private static bool alreadyCheckingWorld;
        private static Dictionary<string, bool> checkedWorlds = new Dictionary<string, bool>();

        internal static IEnumerator CheckWorld()
        {
            if (alreadyCheckingWorld)
            {
                MelonLogger.Error("Attempted to check for world multiple times");
                yield break;
            }

            var worldId = RoomManager.field_Internal_Static_ApiWorld_0.id;

            if (checkedWorlds.ContainsKey(worldId))
            {
                MainClass.ForceDisable = checkedWorlds[worldId];
                MelonLogger.Msg($"Using cached check {checkedWorlds[worldId]} for world '{worldId}'");
                yield break;
            }

            alreadyCheckingWorld = true;

            // Check if black/whitelisted from EmmVRC - thanks Emilia and the rest of EmmVRC Staff
            // [Lil Fluff] - Changed this from Unity WWW to a task based async System.Net WebRequest.
            //   -- The Unity WWW module became obsolete in Unity 2019.

            HttpWebRequest request = WebRequest.CreateHttp($"https://dl.emmvrc.com/riskyfuncs.php?worldid={worldId}");

            Task<WebResponse> getResponse = request.GetResponseAsync();
            while (!getResponse.IsCompleted)
                yield return new WaitForEndOfFrame();

            var result = (HttpWebResponse)getResponse.Result;

            if (result.StatusCode == HttpStatusCode.OK) {
                using (var stream = result.GetResponseStream())
                using (var reader = new StreamReader(stream)) {
                    var parsedText = reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(parsedText)) {
                        switch (parsedText) {
                            case "allowed":
                                MainClass.ForceDisable = false;
                                checkedWorlds.Add(worldId,false);
                                alreadyCheckingWorld = false;
                                MelonLogger.Msg($"EmmVRC allows world '{worldId}'");
                                yield break;

                            case "denied":
                                MainClass.ForceDisable = true;
                                checkedWorlds.Add(worldId,true);
                                alreadyCheckingWorld = false;
                                MelonLogger.Msg($"EmmVRC denies world '{worldId}'");
                                yield break;
                        }
                    }
                }
            }
            // no result from server or they're currently down
            // Check tags then. should also be in cache as it just got downloaded
            API.Fetch<ApiWorld>(
                worldId,
                new Action<ApiContainer>(
                    container =>
                    {
                        ApiWorld apiWorld;
                        if ((apiWorld = container.Model.TryCast<ApiWorld>()) != null)
                        {
                            foreach (var worldTag in apiWorld.tags)
                                if (worldTag.IndexOf("game", StringComparison.OrdinalIgnoreCase) != -1
                                    || worldTag.IndexOf("club", StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    MainClass.ForceDisable = true;
                                    checkedWorlds.Add(worldId, true);
                                    alreadyCheckingWorld = false;
                                    MelonLogger.Msg($"Found game or club tag in world '{worldId}'");
                                    return;
                                }
                            MainClass.ForceDisable = false;
                            checkedWorlds.Add(worldId, false);
                            alreadyCheckingWorld = false;
                            MelonLogger.Msg($"Found no game or club tag in world '{worldId}'");
                        }
                        else
                        {
                            MelonLogger.Error("Failed to cast ApiModel to ApiWorld");
                        }
                    }),
                disableCache: false);
        }
    }
}
