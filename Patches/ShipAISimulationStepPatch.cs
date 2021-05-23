using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace MovableBridge {
    [HarmonyPatch]
    public static class ShipAISimulationStepPatch {
        public static MethodBase TargetMethod() {
            // SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
            return typeof(ShipAI).GetMethod("SimulationStep", BindingFlags.Instance | BindingFlags.Public, Type.DefaultBinder, new[] {
                typeof(ushort),
                typeof(Vehicle).MakeByRefType(),
                typeof(Vehicle.Frame).MakeByRefType(),
                typeof(ushort),
                typeof(Vehicle).MakeByRefType(),
                typeof(int)
            }, null);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var vector3normalizedGetter = typeof(Vector3).GetProperty("normalized").GetGetMethod();
            var netInfoClassField = typeof(NetInfo).GetField("m_class");
            var itemClassServiceField = typeof(ItemClass).GetField("m_service");
            if (vector3normalizedGetter == null || netInfoClassField == null || itemClassServiceField == null) {
                Debug.Log("Getting fields failed...");
                return instructions;
            }

            var getSideOffsetMethodInfo = typeof(ShipAISimulationStepPatch).GetMethod(nameof(GetSideOffset), BindingFlags.NonPublic | BindingFlags.Static);
            if (getSideOffsetMethodInfo == null) {
                Debug.Log("Getting getSideOffsetMethodInfo failed...");
                return instructions;
            }

            var codes = new List<CodeInstruction>(instructions);

            var getSideOffsetInserted = false;
            for (int i = 0; i < codes.Count; i++) {
                // vector5 = vector5.normalized * 20f;
                if (
                    codes[i].IsLdloc() &&
                    codes[i + 1].Is(OpCodes.Call, vector3normalizedGetter) &&
                    codes[i + 2].Is(OpCodes.Ldc_R4, 20f) &&
                    codes[i + 3].opcode == OpCodes.Call && 
                    codes[i + 4].IsStloc()) {
                    Debug.Log("Injecting ship GetSideOffset");

                    var newCodes = new[] {
                        new CodeInstruction(codes[i + 1]) {
                            opcode = OpCodes.Ldarg_1,
                            operand = null
                        },
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldarg_3),
                        new CodeInstruction(OpCodes.Call, getSideOffsetMethodInfo),
                    };
                    codes.RemoveAt(i + 2);
                    codes.InsertRange(i + 2, newCodes);
                    getSideOffsetInserted = true;
                    break;
                }
            }
            if (!getSideOffsetInserted) {
                Debug.Log("ship GetSideOffset injection failed!");
                return codes;
            }

            return codes;
        }

        private static float GetSideOffset(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData) {
            VehicleInfo vehicleInfo = vehicleData.Info;
            Vector3 position = frameData.m_position;
            Vector2 xz = VectorUtils.XZ(frameData.m_position);
            float y = position.y;
            Quaternion rotation = frameData.m_rotation;
            Vector3 size = vehicleInfo.m_generatedInfo.m_size;

            float vehicleTopY = y + vehicleInfo.m_generatedInfo.m_size.y - vehicleInfo.m_generatedInfo.m_negativeHeight;
            Vector2 forwardDir = VectorUtils.XZ(rotation * Vector3.forward).normalized;
            Vector2 rightDir = VectorUtils.XZ(rotation * Vector3.right).normalized;
            Quad2 passingQuad = new Quad2 {
                a = xz - 0.5f * size.z * forwardDir - 0.5f * size.x * rightDir,
                b = xz - 0.5f * size.z * forwardDir + 0.5f * size.x * rightDir,
                c = xz + 0.75f * size.z * forwardDir + 0.5f * size.x * rightDir,
                d = xz + 0.75f * size.z * forwardDir - 0.5f * size.x * rightDir
            };

            float halfWidth = size.x / 2;
            Quad2 quad01 = QuadUtils.GetSegmentQuad(vehicleData.m_targetPos0, vehicleData.m_targetPos1, halfWidth);
            Quad2 quad12 = QuadUtils.GetSegmentQuad(vehicleData.m_targetPos1, vehicleData.m_targetPos2, halfWidth);

            Vector2 quadMin = Vector2.Min(Vector2.Min(passingQuad.Min(), quad01.Min()), quad12.Min());
            Vector2 quadMax = Vector2.Max(Vector2.Max(passingQuad.Max(), quad01.Max()), quad12.Max());
            float yMin = Mathf.Min(Mathf.Min(vehicleData.m_targetPos0.y, vehicleData.m_targetPos1.y),
                Mathf.Min(vehicleData.m_targetPos2.y, vehicleData.m_targetPos3.y));
            float yMax = Mathf.Max(Mathf.Max(vehicleData.m_targetPos0.y, vehicleData.m_targetPos1.y),
                Mathf.Max(vehicleData.m_targetPos2.y, vehicleData.m_targetPos3.y));

            int minGridX = Math.Max((int)((quadMin.x - 72f) / 64f + 135f), 0);
            int minGridZ = Math.Max((int)((quadMin.y - 72f) / 64f + 135f), 0);
            int maxGridX = Math.Min((int)((quadMax.x + 72f) / 64f + 135f), 269);
            int maxGridZ = Math.Min((int)((quadMax.y + 72f) / 64f + 135f), 269);
            float minY = yMin - vehicleInfo.m_generatedInfo.m_negativeHeight - 2f;
            float maxY = yMax + vehicleInfo.m_generatedInfo.m_size.y + 2f;
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            for (int gridZ = minGridZ; gridZ <= maxGridZ; gridZ++) {
                for (int gridX = minGridX; gridX <= maxGridX; gridX++) {
                    ushort buildingID = buildingManager.m_buildingGrid[gridZ * 270 + gridX];
                    while (buildingID != 0) {
                        bool overlap01 = buildingManager.m_buildings.m_buffer[buildingID].OverlapQuad(buildingID, quad01, minY, maxY, ItemClass.CollisionType.Terrain);
                        bool overlap02 = buildingManager.m_buildings.m_buffer[buildingID].OverlapQuad(buildingID, quad12, minY, maxY, ItemClass.CollisionType.Terrain);

                        if (overlap01 || overlap02) {
                            return 0f;
                        }

                        buildingID = buildingManager.m_buildings.m_buffer[buildingID].m_nextGridBuilding;
                    }
                }
            }
            return 20f;
        }
    }
}