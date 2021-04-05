using HarmonyLib;
using UnityEngine;

namespace MovableBridge {
    [HarmonyPatch(typeof(BuildingDecoration), "SavePaths")]
    public static class BuildingDecorationSavePathsPatch {
        public static void Postfix(BuildingInfo info, ushort buildingID, ref Building data) {
            if (info.m_paths == null) return;

            foreach (var path in info.m_paths) {
                Debug.Log($"path: {path.m_finalNetInfo.name}");
                foreach (var node in path.m_nodes) {
                    Debug.Log($"node: {node}");
                }

                for (var n = 0; n < path.m_nodes.Length; n++) {
                    if (path.m_nodes[n].x == -36 || path.m_nodes[n].x == 36) {
                        Debug.Log($"patching node: {path.m_nodes[n]}");
                        path.m_trafficLights[n] = BuildingInfo.TrafficLights.ForceOn;
                    }
                }
            }
        }
    }
}