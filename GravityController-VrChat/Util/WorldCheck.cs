using System;
using System.Collections;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;
using VRC.Core;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace GravityController.Util {
    // Mostly stolen from https://github.com/Psychloor/PlayerRotater/blob/0b30e04cf85fdab769f6e0afc020e6d9bc9900ac/PlayerRotater/Utilities.cs#L76
    class WorldCheck {

        private static bool alreadyCheckingWorld;
        private static Dictionary<string,bool> checkedWorlds = new Dictionary<string,bool>();

        internal static IEnumerator CheckWorld() {
            if (alreadyCheckingWorld) {
                MelonLogger.Error("Attempted to check for world multiple times");
                yield break;
            }

            var worldId = RoomManager.field_Internal_Static_ApiWorld_0.id;

            if (checkedWorlds.ContainsKey(worldId)) {
                GravityMod.ForceDisable = checkedWorlds[worldId];
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

            var result = (HttpWebResponse) getResponse.Result;

            if (result.StatusCode == HttpStatusCode.OK) {
                using (var stream = result.GetResponseStream())
                using (var reader = new StreamReader(stream)) {
                    var parsedText = reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(parsedText)) {
                        switch (parsedText) {
                            case "allowed":
                                GravityMod.ForceDisable = false;
                                checkedWorlds.Add(worldId,false);
                                alreadyCheckingWorld = false;
                                MelonLogger.Msg($"EmmVRC allows world '{worldId}'");
                                yield break;

                            case "denied":
                                GravityMod.ForceDisable = true;
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
                    container => {
                        ApiWorld apiWorld;
                        if ((apiWorld = container.Model.TryCast<ApiWorld>()) != null) {
                            foreach (var worldTag in apiWorld.tags)
                                if (worldTag.IndexOf("game",StringComparison.OrdinalIgnoreCase) != -1
                                    || worldTag.IndexOf("club",StringComparison.OrdinalIgnoreCase) != -1) {
                                    GravityMod.ForceDisable = true;
                                    checkedWorlds.Add(worldId,true);
                                    alreadyCheckingWorld = false;
                                    MelonLogger.Msg($"Found game or club tag in world '{worldId}'");
                                    return;
                                }
                            GravityMod.ForceDisable = false;
                            checkedWorlds.Add(worldId,false);
                            alreadyCheckingWorld = false;
                            MelonLogger.Msg($"Found no game or club tag in world '{worldId}'");
                        } else {
                            MelonLogger.Error("Failed to cast ApiModel to ApiWorld");
                        }
                    }),
                disableCache: false);
        }
    }
}
