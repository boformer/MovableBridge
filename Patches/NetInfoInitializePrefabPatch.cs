using HarmonyLib;

namespace MovableBridge {
    [HarmonyPatch(typeof(NetInfo), "InitializePrefab")]
    public static class NetInfoInitializePrefabPatch {
        public static void Prefix(NetInfo __instance) {
            if (!Mod.IsInGame) return;

            if (__instance.mapEditorCategory == "MovableBridge_Movable" || __instance.mapEditorCategory == "MovableBridge_Static") {

                UnityEngine.Debug.Log($"Adding MovableBridgeRoadAI to ${__instance.name}");

                NetAI oldAI = __instance.gameObject.GetComponent<NetAI>();
                MovableBridgeRoadAI newAI = __instance.gameObject.AddComponent<MovableBridgeRoadAI>();
                newAI.GetCopyOf(oldAI);

                UnityEngine.Object.DestroyImmediate(oldAI);
            }
        }
    }
}
