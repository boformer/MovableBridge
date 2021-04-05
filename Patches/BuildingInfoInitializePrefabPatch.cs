using HarmonyLib;

namespace MovableBridge {
    [HarmonyPatch(typeof(BuildingInfo), "InitializePrefab")]
    public static class BuildingInfoInitializePrefabPatch {
        public static void Prefix(BuildingInfo __instance) {
            if (!Mod.IsInGame) return;

            if (__instance.name == "draw_bridge_large.Draw Bridge Large_Data" || __instance.name == "Palace Bridge.Palace Bridge_Data") { // TODO properly detect draw bridges
                UnityEngine.Debug.Log($"Adding MovableBridgeAI to ${__instance.name}");

                BuildingAI oldAI = __instance.gameObject.GetComponent<BuildingAI>();
                UnityEngine.Object.DestroyImmediate(oldAI);

                MovableBridgeAI newAI = __instance.gameObject.AddComponent<MovableBridgeAI>();

                newAI.m_electricityConsumption = 0;
                newAI.m_waterConsumption = 0;
                newAI.m_sewageAccumulation = 0;
                newAI.m_garbageAccumulation = 0;

                // disable building LOD
                __instance.m_lodObject = null;

                foreach (BuildingInfo.PathInfo pathInfo in __instance.m_paths) {
                    if (pathInfo.m_netInfo.name.EndsWith("(Movable)") && pathInfo.m_nodes.Length > 1) {
                        if(pathInfo.m_trafficLights == null) 
                            pathInfo.m_trafficLights = new BuildingInfo.TrafficLights[pathInfo.m_nodes.Length];
                        pathInfo.m_trafficLights[0] = BuildingInfo.TrafficLights.ForceOn;
                        pathInfo.m_trafficLights[pathInfo.m_trafficLights.Length - 1] = BuildingInfo.TrafficLights.ForceOn;
                    }
                }
            }
        }
    }
}
