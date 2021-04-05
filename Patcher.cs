using HarmonyLib;
using UnityEngine;

namespace MovableBridge {
    public static class Patcher {
        private const string kHarmonyId = "boformer.MovableBridge";

        public static void Patch() {
            Debug.Log("MovableBridge Patching...");
            var harmony = new Harmony(kHarmonyId);
            harmony.PatchAll(typeof(Patcher).Assembly);
        }

        public static void Unpatch() {
            var harmony = new Harmony(kHarmonyId);
            harmony.UnpatchAll(kHarmonyId);
            Debug.Log("MovableBridge Reverted...");
        }
    }
}
