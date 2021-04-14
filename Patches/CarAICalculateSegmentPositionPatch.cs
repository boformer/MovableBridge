using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MovableBridge {
    [HarmonyPatch]
    public static class CarAICalculateSegmentPositionPatch {
        public static MethodBase TargetMethod() {
            // CalculateSegmentPosition(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position nextPosition, PathUnit.Position position, uint laneID, byte offset, PathUnit.Position prevPos, uint prevLaneID, byte prevOffset, int index, out Vector3 pos, out Vector3 dir, out float maxSpeed)
            return typeof(CarAI).GetMethod("CalculateSegmentPosition", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, new [] {
                    typeof(ushort), 
                    typeof(Vehicle).MakeByRefType(), 
                    typeof(PathUnit.Position),
                    typeof(PathUnit.Position),
                    typeof(uint), 
                    typeof(byte),
                    typeof(PathUnit.Position),
                    typeof(uint),
                    typeof(byte),
                    typeof(int),
                    typeof(Vector3).MakeByRefType(),
                    typeof(Vector3).MakeByRefType(), 
                    typeof(float).MakeByRefType()
                }, null);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var vehicleFlagsField = typeof(Vehicle).GetField("m_flags");
            var netInfoClassField = typeof(NetInfo).GetField("m_class");
            var itemClassServiceField = typeof(ItemClass).GetField("m_service");
            if (vehicleFlagsField == null || netInfoClassField == null || itemClassServiceField == null) {
                Debug.Log("Getting fields failed...");
                return instructions;
            }

            var codes = new List<CodeInstruction>(instructions);

            var emergencyTrafficLightCheckFound = false;
            for (int i = 0; i < codes.Count; i++) {
                // // if ((vehicleData.m_flags & Vehicle.Flags.Emergency2) == 0 || info.m_class.m_service != ItemClass.Service.Road)
                if (
                    codes[i].opcode == OpCodes.Ldarg_2 && 
                    codes[i + 1].Is(OpCodes.Ldfld, vehicleFlagsField) &&
                    codes[i + 2].Is(OpCodes.Ldc_I4, 128) &&
                    codes[i + 3].opcode == OpCodes.And &&
                    codes[i + 4].opcode == OpCodes.Brfalse &&
                    codes[i + 5].IsLdloc() &&
                    codes[i + 6].Is(OpCodes.Ldfld, netInfoClassField) &&
                    codes[i + 7].Is(OpCodes.Ldfld, itemClassServiceField) &&
                    codes[i + 8].Is(OpCodes.Ldc_I4_S, (byte)9) &&
                    codes[i + 9].opcode == OpCodes.Beq) {

                    Debug.Log("Inserting emergency vehicle draw bridge check");
                    // Insert another condition: IsMovableBridge(nodeNetInfo)
                    codes.InsertRange(i + 5, GetCodeInstructions(codes[i + 5], (Label)codes[i + 4].operand));
                    emergencyTrafficLightCheckFound = true;
                    break;
                }
            }
            if (!emergencyTrafficLightCheckFound) {
                Debug.Log("emergencyTrafficLightCheck not found!");
                return codes;
            }

            return codes;
        }

        static IEnumerable<CodeInstruction> GetCodeInstructions(CodeInstruction ldNodeNetInfoInstruction, Label trafficLightCheckLabel) {
            var isMovableBridgeMethodInfo = typeof(CarAICalculateSegmentPositionPatch).GetMethod(nameof(IsMovableBridge), BindingFlags.NonPublic | BindingFlags.Static);
            if (isMovableBridgeMethodInfo == null) {
                Debug.Log("Getting isMovableBridgeMethodInfo failed...");
                yield break;
            }

            yield return new CodeInstruction(ldNodeNetInfoInstruction.opcode, ldNodeNetInfoInstruction.operand);
            yield return new CodeInstruction(OpCodes.Call, isMovableBridgeMethodInfo);
            yield return new CodeInstruction(OpCodes.Brtrue, trafficLightCheckLabel);
        }

        private static bool IsMovableBridge(NetInfo netInfo) {
            return netInfo.m_netAI is MovableBridgeRoadAI;
        }
    }
}