#if DEBUG
using System;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace MovableBridge {
    [HarmonyPatch(typeof(Vehicle), "RenderOverlay")]
    public class VehicleRenderOverlayPatch {
        public static void Postfix(RenderManager.CameraInfo cameraInfo, ushort vehicleID, Color color) {
            Vehicle vehicleData = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID];
            VehicleInfo vehicleInfo = vehicleData.Info;
            if (vehicleInfo.m_vehicleType != VehicleInfo.VehicleType.Ship && vehicleInfo.m_vehicleType != VehicleInfo.VehicleType.Ferry) return;

            uint targetFrame = GetTargetFrame(ref vehicleData, vehicleInfo, vehicleID);
            Vehicle.Frame frameData = vehicleData.GetFrameData(targetFrame - 16);
            Vector3 position = frameData.m_position;
            Vector2 xz = VectorUtils.XZ(frameData.m_position);
            float y = position.y;
            Quaternion rotation = frameData.m_rotation;
            Vector3 size = vehicleData.Info.m_generatedInfo.m_size;
            Vector2 forwardDir = VectorUtils.XZ(rotation * Vector3.forward).normalized;
            Vector2 rightDir = VectorUtils.XZ(rotation * Vector3.right).normalized;
            float circleMinY = y - vehicleInfo.m_generatedInfo.m_negativeHeight - 50f;
            float circleMaxY = y + vehicleInfo.m_generatedInfo.m_size.y + 50f;

            Quad2 passingQuad = new Quad2 {
                a = xz - 0.5f * size.z * forwardDir - 0.5f * size.x * rightDir,
                b = xz - 0.5f * size.z * forwardDir + 0.5f * size.x * rightDir,
                c = xz + 0.75f * size.z * forwardDir + 0.5f * size.x * rightDir,
                d = xz + 0.75f * size.z * forwardDir - 0.5f * size.x * rightDir
            };
            DrawSearchConeQuad(cameraInfo, circleMinY, circleMaxY, passingQuad);

            RenderPos(vehicleData.GetTargetPos(0), vehicleInfo, cameraInfo);
            RenderPos(vehicleData.GetTargetPos(1), vehicleInfo, cameraInfo);
            RenderPos(vehicleData.GetTargetPos(2), vehicleInfo, cameraInfo);
            RenderPos(vehicleData.GetTargetPos(3), vehicleInfo, cameraInfo);

            float halfWidth = vehicleData.Info.m_generatedInfo.m_size.x / 2;
            Quad2 quad01 = GetShipQuad(vehicleData.m_targetPos0, vehicleData.m_targetPos1, halfWidth);
            Quad2 quad12 = GetShipQuad(vehicleData.m_targetPos1, vehicleData.m_targetPos2, halfWidth);
            Quad2 quad23 = GetShipQuad(vehicleData.m_targetPos2, vehicleData.m_targetPos3, halfWidth);
            DrawSearchConeQuad2(cameraInfo, circleMinY, circleMaxY, quad01);
            DrawSearchConeQuad2(cameraInfo, circleMinY, circleMaxY, quad12);
            DrawSearchConeQuad2(cameraInfo, circleMinY, circleMaxY, quad23);

            Vector2 quadMin = Vector2.Min(Vector2.Min(passingQuad.Min(), quad01.Min()), Vector2.Min(quad12.Min(), quad23.Min()));
            Vector2 quadMax = Vector2.Max(Vector2.Max(passingQuad.Max(), quad01.Max()), Vector2.Max(quad12.Max(), quad23.Max()));
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
                        bool overlap01 = buildingManager.m_buildings.m_buffer[buildingID].OverlapQuad(buildingID,
                            quad01, minY, maxY, ItemClass.CollisionType.Terrain);
                        bool overlap12 = buildingManager.m_buildings.m_buffer[buildingID].OverlapQuad(buildingID,
                            quad12, minY, maxY, ItemClass.CollisionType.Terrain);
                        bool overlap23 = buildingManager.m_buildings.m_buffer[buildingID].OverlapQuad(buildingID,
                            quad23, minY, maxY, ItemClass.CollisionType.Terrain);
                        bool overlap = overlap01 || overlap12 || overlap23;

                        Color color2 = (overlap ? Color.magenta : Color.white);
                        BuildingTool.RenderOverlay(cameraInfo, ref buildingManager.m_buildings.m_buffer[buildingID], color2, color2);
                        buildingID = buildingManager.m_buildings.m_buffer[buildingID].m_nextGridBuilding;
                    }
                }
            }

            //float searchConeLength = (vehicleInfo.m_vehicleType == VehicleInfo.VehicleType.Ship)
            //    ? ShipAICheckOtherVehiclesPatch.kSearchConeLength
            //    : FerryAICheckOtherVehiclesPatch.kSearchConeLength;

            //uint targetFrame = GetTargetFrame(ref vehicleData, vehicleInfo, vehicleID);
            //Vehicle.Frame frameData = vehicleData.GetFrameData(targetFrame - 16);
            //Vector3 position = frameData.m_position;
            //Vector2 xz = VectorUtils.XZ(frameData.m_position);
            //float y = position.y;
            //Quaternion rotation = frameData.m_rotation;
            //Vector3 size = vehicleData.Info.m_generatedInfo.m_size;
            //Vector2 forwardDir = VectorUtils.XZ(rotation * Vector3.forward).normalized;
            //Vector2 rightDir = VectorUtils.XZ(rotation * Vector3.right).normalized;
            //float circleMinY = y - vehicleInfo.m_generatedInfo.m_negativeHeight - 50f;
            //float circleMaxY = y + vehicleInfo.m_generatedInfo.m_size.y + 50f;

            //Quad2 searchConeQuad = new Quad2 {
            //    a = xz - 0.5f * size.z * forwardDir - 0.5f * size.x * rightDir,
            //    b = xz - 0.5f * size.z * forwardDir + 0.5f * size.x * rightDir,
            //    c = xz + (0.5f * size.z + searchConeLength) * forwardDir + 2f * size.x * rightDir,
            //    d = xz + (0.5f * size.z + searchConeLength) * forwardDir - 2f * size.x * rightDir
            //};
            //DrawSearchConeQuad(cameraInfo, circleMinY, circleMaxY, searchConeQuad);

            //Quad2 passingQuad = new Quad2 {
            //    a = xz - 0.5f * size.z * forwardDir - 0.5f * size.x * rightDir,
            //    b = xz - 0.5f * size.z * forwardDir + 0.5f * size.x * rightDir,
            //    c = xz + 1f * size.z * forwardDir + 0.5f * size.x * rightDir,
            //    d = xz + 1f * size.z * forwardDir - 0.5f * size.x * rightDir
            //};
            //DrawSearchConeQuad(cameraInfo, circleMinY, circleMaxY, passingQuad);

            //Vector2 searchConeMin = searchConeQuad.Min();
            //Vector2 searchConeMax = searchConeQuad.Max();
            //Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, Color.yellow, VectorUtils.X_Y(searchConeMin), 20f, circleMinY, circleMaxY, renderLimits: false, alphaBlend: true);
            //Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, Color.yellow, VectorUtils.X_Y(searchConeMax), 20f, circleMinY, circleMaxY, renderLimits: false, alphaBlend: true);

            //int minGridX = Math.Max((int)((searchConeMin.x - 72f) / 64f + 135f), 0);
            //int minGridZ = Math.Max((int)((searchConeMin.y - 72f) / 64f + 135f), 0);
            //int maxGridX = Math.Min((int)((searchConeMax.x + 72f) / 64f + 135f), 269);
            //int maxGridZ = Math.Min((int)((searchConeMax.y + 72f) / 64f + 135f), 269);
            //DrawBuildingGridRange(cameraInfo, circleMinY, circleMaxY, minGridX, minGridZ, maxGridX, maxGridZ);

            //float minY = y - vehicleInfo.m_generatedInfo.m_negativeHeight - 2f;
            //float maxY = y + vehicleInfo.m_generatedInfo.m_size.y + 2f;
            //BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            //for (int gridZ = minGridZ; gridZ <= maxGridZ; gridZ++) {
            //    for (int gridX = minGridX; gridX <= maxGridX; gridX++) {
            //        ushort buildingID = buildingManager.m_buildingGrid[gridZ * 270 + gridX];
            //        while (buildingID != 0) {
            //            Color color2 = (buildingManager.m_buildings.m_buffer[buildingID].OverlapQuad(buildingID, searchConeQuad, minY, maxY, ItemClass.CollisionType.Terrain) ? Color.magenta : Color.white);
            //            BuildingTool.RenderOverlay(cameraInfo, ref buildingManager.m_buildings.m_buffer[buildingID], color2, color2);
            //            buildingID = buildingManager.m_buildings.m_buffer[buildingID].m_nextGridBuilding;
            //        }
            //    }
            //}
        }

        private static Quad2 GetShipQuad(Vector3 a, Vector3 b, float halfWidth) {
            Vector2 forwardDir = VectorUtils.XZ(b - a).normalized;
            Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);
            return new Quad2 {
                a = VectorUtils.XZ(a) - halfWidth * rightDir,
                b = VectorUtils.XZ(a) + halfWidth * rightDir,
                c = VectorUtils.XZ(b) + halfWidth * rightDir,
                d = VectorUtils.XZ(b) - halfWidth * rightDir
            };
        }

        private static void RenderPos(Vector3 pos, VehicleInfo vehicleInfo, RenderManager.CameraInfo cameraInfo) {
            var y = pos.y;
            float circleMinY = y - vehicleInfo.m_generatedInfo.m_negativeHeight - 50f;
            float circleMaxY = y + vehicleInfo.m_generatedInfo.m_size.y + 50f;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, Color.yellow, pos, 20f, circleMinY, circleMaxY, renderLimits: false, alphaBlend: true);
        }

        private static void DrawSearchConeQuad(RenderManager.CameraInfo cameraInfo, float minOverlayY, float maxOverlayY, Quad2 searchConeQuad) {
            Quad3 searchConeQuad3 = new Quad3 {
                a = VectorUtils.X_Y(searchConeQuad.a),
                b = VectorUtils.X_Y(searchConeQuad.b),
                c = VectorUtils.X_Y(searchConeQuad.c),
                d = VectorUtils.X_Y(searchConeQuad.d)
            };
            Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, Color.red, searchConeQuad3, minOverlayY, maxOverlayY, renderLimits: false, alphaBlend: true);
        }

        private static void DrawSearchConeQuad2(RenderManager.CameraInfo cameraInfo, float minOverlayY, float maxOverlayY, Quad2 searchConeQuad) {
            Quad3 searchConeQuad3 = new Quad3 {
                a = VectorUtils.X_Y(searchConeQuad.a),
                b = VectorUtils.X_Y(searchConeQuad.b),
                c = VectorUtils.X_Y(searchConeQuad.c),
                d = VectorUtils.X_Y(searchConeQuad.d)
            };
            Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, Color.yellow, searchConeQuad3, minOverlayY, maxOverlayY, renderLimits: false, alphaBlend: true);
        }

        private static void DrawBuildingGridRange(RenderManager.CameraInfo cameraInfo, float minOverlayY, float maxOverlayY, int minX, int minZ, int maxX, int maxZ) {
            Quad3 gridQuad = new Quad3 {
                a = new Vector3((float)(minX - 135) * 64f, 0f, (float)(minZ - 135) * 64f),
                b = new Vector3((float)(minX - 135) * 64f, 0f, (float)(maxZ - 135 + 1) * 64f),
                c = new Vector3((float)(maxX - 135 + 1) * 64f, 0f, (float)(maxZ - 135 + 1) * 64f),
                d = new Vector3((float)(maxX - 135 + 1) * 64f, 0f, (float)(minZ - 135) * 64f)
            };
            Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, Color.yellow, gridQuad, minOverlayY, maxOverlayY, renderLimits: false, alphaBlend: true);
        }

        private static uint GetTargetFrame(ref Vehicle vehicleData, VehicleInfo info, ushort vehicleID) {
            ushort firstVehicle = vehicleData.GetFirstVehicle(vehicleID);
            uint num = (uint)(firstVehicle << 4) / 16384u;
            return Singleton<SimulationManager>.instance.m_referenceFrameIndex - num;
        }
    }
}
#endif
