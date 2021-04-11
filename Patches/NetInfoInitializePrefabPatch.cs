using HarmonyLib;

namespace MovableBridge {
    [HarmonyPatch(typeof(NetInfo), "InitializePrefab")]
    public static class NetInfoInitializePrefabPatch {
        public static void Prefix(NetInfo __instance) {
            if (!Mod.IsInGame) return;

            bool movableNet = (__instance.editorCategory == "MovableBridge_Movable");
            bool staticNet = (__instance.editorCategory == "MovableBridge_Static");

            if (movableNet || staticNet) {

                UnityEngine.Debug.Log($"Adding MovableBridgeRoadAI to ${__instance.name}");

                NetAI oldAI = __instance.gameObject.GetComponent<NetAI>();

                if (oldAI is RoadBridgeAI) {
                    MovableBridgeRoadAI newAI = __instance.gameObject.AddComponent<MovableBridgeRoadAI>();
                    newAI.CopyFrom(oldAI);
                    newAI.m_Movable = movableNet;

                    UnityEngine.Object.DestroyImmediate(oldAI);
                } else if (oldAI is TrainTrackBridgeAI) {
                    MovableBridgeTrainTrackAI newAI = __instance.gameObject.AddComponent<MovableBridgeTrainTrackAI>();
                    newAI.CopyFrom(oldAI);
                    newAI.m_Movable = movableNet;

                    UnityEngine.Object.DestroyImmediate(oldAI);
                }
            }
        }
    }
}
