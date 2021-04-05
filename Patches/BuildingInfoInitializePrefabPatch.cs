using HarmonyLib;

namespace MovableBridge {
    [HarmonyPatch(typeof(BuildingInfo), "InitializePrefab")]
    public static class BuildingInfoInitializePrefabPatch {
        public static void Prefix(BuildingInfo __instance) {
            if (__instance.name == "draw_bridge_large.Draw Bridge Large_Data" || __instance.name == "Palace Bridge.Palace Bridge_Data") { // TODO properly detect draw bridges
                var oldAi = __instance.gameObject.GetComponent<BuildingAI>();
                UnityEngine.Object.DestroyImmediate(oldAi);

                var newAi = __instance.gameObject.AddComponent<MovableBridgeAI>();

                newAi.m_electricityConsumption = 0;
                newAi.m_waterConsumption = 0;
                newAi.m_sewageAccumulation = 0;
                newAi.m_garbageAccumulation = 0;

                UnityEngine.Debug.Log($"Building AI replaced!");

                __instance.m_lodObject = null;
            }
        }
    }
}
