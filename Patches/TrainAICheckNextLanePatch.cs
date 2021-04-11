using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MovableBridge {
    [HarmonyPatch(typeof(TrainAI), "CheckNextLane")]
    public static class TrainAICheckNextLanePatch {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);

            bool brakingDistanceSubPatched = false;
            for (int i = 0; i < codes.Count; i++) {
                if (CodeInstructionExtensions.IsLdloc(codes[i]) && codes[i + 1].opcode == OpCodes.Ldc_R4 && codes[i + 1].operand.Equals(5) && codes[i + 2].opcode == OpCodes.Sub) {
                    codes[i + 2].opcode = OpCodes.Add;
                    brakingDistanceSubPatched = true;
                    Debug.Log("brakingDistanceSub found");
                    break;
                }
            }

            if (!brakingDistanceSubPatched) {
                Debug.Log("brakingDistanceSub not found!");
                return codes;
            }

            return codes;
        }
    }
}
