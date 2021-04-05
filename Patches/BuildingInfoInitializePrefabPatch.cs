using HarmonyLib;

namespace MovableBridge {
    [HarmonyPatch(typeof(BuildingInfo), "InitializePrefab")]
    public static class BuildingInfoInitializePrefabPatch {
        public static void Prefix(BuildingInfo __instance) {
            if (!Mod.IsInGame && !Mod.IsInAssetEditor) return;

            if (__instance.editorCategory == "MovableBridge") {
                UnityEngine.Debug.Log($"Adding MovableBridgeAI to ${__instance.name}");

                BuildingAI oldAI = __instance.gameObject.GetComponent<BuildingAI>();
                MovableBridgeAI newAI = __instance.gameObject.AddComponent<MovableBridgeAI>();
                newAI.m_electricityConsumption = 0;
                newAI.m_waterConsumption = 0;
                newAI.m_sewageAccumulation = 0;
                newAI.m_garbageAccumulation = 0;
                newAI.CopyFrom(oldAI);

                UnityEngine.Object.DestroyImmediate(oldAI);

                // disable building LOD
                __instance.m_lodObject = null;

                foreach (BuildingInfo.PathInfo pathInfo in __instance.m_paths) {
                    if (pathInfo.m_netInfo.editorCategory == "MovableBridge_Movable" && pathInfo.m_nodes.Length > 1) {
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
