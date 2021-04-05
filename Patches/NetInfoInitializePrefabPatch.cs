using HarmonyLib;

namespace MovableBridge {
    [HarmonyPatch(typeof(NetInfo), "InitializePrefab")]
    public static class NetInfoInitializePrefabPatch {
        public static void Prefix(NetInfo __instance) {
            if (__instance.name == "draw_bridge_large_road.Draw Bridge Large Road_Data"
                || __instance.name == "draw_bridge_large_road_inv.Draw Bridge Large Road Inv_Data") { // TODO properly detect draw bridges
                var oldAi = __instance.gameObject.GetComponent<NetAI>();
                MovableBridgeRoadAI newAi = __instance.gameObject.AddComponent<MovableBridgeRoadAI>().GetCopyOf(oldAi);

                UnityEngine.Object.DestroyImmediate(oldAi);

                UnityEngine.Debug.Log($"Net AI replaced!");
            }
        }
    }
}
