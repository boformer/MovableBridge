using System;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace MovableBridge {
    [HarmonyPatch(typeof(FerryAI), "CheckOtherVehicles")]
    public static class FerryAICheckOtherVehiclesPatch {
        public const float kSearchConeLength = 100f;
        private const float kPassingSpeed = 3f;
        private const float kOpeningSpeed = 1f;

        public static void Prefix(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, float maxDistance, float maxBraking) {
            VehicleInfo vehicleInfo = vehicleData.Info;
            Vector3 position = frameData.m_position;
            Vector2 xz = VectorUtils.XZ(frameData.m_position);
            float y = position.y;
            Quaternion rotation = frameData.m_rotation;
            Vector3 size = vehicleInfo.m_generatedInfo.m_size;

            float vehicleTopY = y + vehicleInfo.m_generatedInfo.m_size.y - vehicleInfo.m_generatedInfo.m_negativeHeight;
            Vector2 forwardDir = VectorUtils.XZ(rotation * Vector3.forward).normalized;
            Vector2 rightDir = VectorUtils.XZ(rotation * Vector3.right).normalized;
            Quad2 searchConeQuad = new Quad2 {
                a = xz - 0.5f * size.z * forwardDir - 0.5f * size.x * rightDir,
                b = xz - 0.5f * size.z * forwardDir + 0.5f * size.x * rightDir,
                c = xz + (0.5f * size.z + kSearchConeLength) * forwardDir + 2f * size.x * rightDir,
                d = xz + (0.5f * size.z + kSearchConeLength) * forwardDir - 2f * size.x * rightDir
            };
            Quad2 passingQuad = new Quad2 {
                a = xz - 0.5f * size.z * forwardDir - 0.5f * size.x * rightDir,
                b = xz - 0.5f * size.z * forwardDir + 0.5f * size.x * rightDir,
                c = xz + 1f * size.z * forwardDir + 0.5f * size.x * rightDir,
                d = xz + 1f * size.z * forwardDir - 0.5f * size.x * rightDir
            };

            Vector2 searchConeMin = searchConeQuad.Min();
            Vector2 searchConeMax = searchConeQuad.Max();
            int minGridX = Math.Max((int)((searchConeMin.x - 72f) / 64f + 135f), 0);
            int minGridZ = Math.Max((int)((searchConeMin.y - 72f) / 64f + 135f), 0);
            int maxGridX = Math.Min((int)((searchConeMax.x + 72f) / 64f + 135f), 269);
            int maxGridZ = Math.Min((int)((searchConeMax.y + 72f) / 64f + 135f), 269);
            float minY = y - vehicleInfo.m_generatedInfo.m_negativeHeight - 2f;
            float maxY = y + vehicleInfo.m_generatedInfo.m_size.y + 2f;
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            for (int gridZ = minGridZ; gridZ <= maxGridZ; gridZ++) {
                for (int gridX = minGridX; gridX <= maxGridX; gridX++) {
                    ushort buildingID = buildingManager.m_buildingGrid[gridZ * 270 + gridX];
                    while (buildingID != 0) {
                        float maxSpeedForBridge = HandleMovableBridge(buildingID, ref buildingManager.m_buildings.m_buffer[buildingID], searchConeQuad, passingQuad, minY, maxY, maxSpeed, vehicleTopY);
                        if (maxSpeedForBridge < maxSpeed) {
                            maxSpeed = CalculateMaxSpeed(0f, maxSpeedForBridge, maxBraking);
                        }
                        buildingID = buildingManager.m_buildings.m_buffer[buildingID].m_nextGridBuilding;
                    }
                }
            }
#if DEBUG
            if (InputListener.slowDown) {
                float targetSpeed = Mathf.Min(maxSpeed, 1f);
                maxSpeed = CalculateMaxSpeed(0f, targetSpeed, maxBraking);
            }
#endif
        }

        private static float HandleMovableBridge(ushort buildingID, ref Building buildingData, Quad2 searchConeQuad, Quad2 passingQuad, float minY, float maxY, float maxSpeed, float vehicleTopY) {
            BuildingInfo buildingInfo = buildingData.Info;
            if (!(buildingInfo.m_buildingAI is MovableBridgeAI)) return float.MaxValue;

            MovableBridgeAI movableBridgeAi = (MovableBridgeAI)buildingInfo.m_buildingAI;
            float bridgeClearance = buildingData.m_position.y + movableBridgeAi.m_BridgeClearance;
            if (bridgeClearance > vehicleTopY) {
                return float.MaxValue;
            }
            if (!buildingData.OverlapQuad(buildingID, searchConeQuad, minY, maxY, ItemClass.CollisionType.Terrain)) {
                return float.MaxValue;
            }
            bool passing = buildingData.OverlapQuad(buildingID, passingQuad, minY, maxY, ItemClass.CollisionType.Terrain);
            ushort bridgeState = MovableBridgeAI.GetBridgeState(ref buildingData);
            buildingData.m_customBuffer1 |= MovableBridgeAI.FLAG_SHIP_NEAR_BRIDGE;
            if (passing) {
                buildingData.m_customBuffer1 |= MovableBridgeAI.FLAG_SHIP_PASSING_BRIDGE;
            }
            if (bridgeState == MovableBridgeAI.STATE_BRIDGE_OPEN || (bridgeState == MovableBridgeAI.STATE_BRIDGE_WAITING_FOR_CLOSE && passing)) {
                return float.MaxValue;
            }
            if (!passing) {
                return kPassingSpeed;
            }
            if (bridgeState == MovableBridgeAI.STATE_BRIDGE_OPENING) {
                return kOpeningSpeed;
            }
            return 0f;
        }

        private static float CalculateMaxSpeed(float targetDistance, float targetSpeed, float maxBraking) {
            float num = 0.5f * maxBraking;
            float num2 = num + targetSpeed;
            return Mathf.Sqrt(Mathf.Max(0f, num2 * num2 + 2f * targetDistance * maxBraking)) - num;
        }
    }
}