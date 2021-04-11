using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace MovableBridge {
    [HarmonyPatch(typeof(CitizenAI), "GetPathTargetPosition")]
    public static class CitizenGetPathTargetPositionPatch {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var checkSegmentChangeMethodInfo =
                typeof(CitizenAI).GetMethod("CheckSegmentChange", BindingFlags.NonPublic | BindingFlags.Instance);
            if (checkSegmentChangeMethodInfo == null) {
                Debug.Log("AiReplace Getting checkSegmentChangeMethodInfo failed...");
                return instructions;
            }

            var hookMethodInfo = typeof(NetSegment).GetMethod(
                "CalculateMiddlePoints",
                BindingFlags.Public | BindingFlags.Static,
                Type.DefaultBinder,
                new[]
                {
                    typeof(Vector3),
                    typeof(Vector3),
                    typeof(Vector3),
                    typeof(Vector3),
                    typeof(bool),
                    typeof(bool),
                    typeof(Vector3).MakeByRefType(),
                    typeof(Vector3).MakeByRefType(),
                    typeof(float).MakeByRefType()
                },
                null
            );
            if (hookMethodInfo == null) {
                Debug.Log("AiReplace Getting hookMethodInfo failed...");
                return instructions;
            }

            var codes = new List<CodeInstruction>(instructions);

            Label stopAtRedPedestrianLightLabel = il.DefineLabel();
            var stopAtRedPedestrianLightLabelFound = false;
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand == checkSegmentChangeMethodInfo) {
                    if (codes[i + 1].opcode == OpCodes.Brtrue && codes[i + 2].opcode == OpCodes.Call) {
                        codes[i + 2].labels.Add(stopAtRedPedestrianLightLabel);
                        stopAtRedPedestrianLightLabelFound = true;
                    }
                }
            }
            if (!stopAtRedPedestrianLightLabelFound) {
                Debug.Log("stopAtRedPedestrianLightLabel not found!");
                return codes;
            }

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand == hookMethodInfo) {
                    Debug.Log("AiReplace transpile hook found");

                    codes.InsertRange(i + 1, GetCodeInstructions(stopAtRedPedestrianLightLabel));
                    break;
                }
            }

            return codes;
        }

        static IEnumerable<CodeInstruction> GetCodeInstructions(Label stopAtRedPedestrianLightLabel) {
            var m_segmentFieldInfo = typeof(PathUnit.Position).GetField("m_segment", BindingFlags.Public | BindingFlags.Instance);
            if (m_segmentFieldInfo == null) {
                Debug.Log("AiReplace Getting m_segmentFieldInfo failed...");
                yield break;
            }

            var m_offsetFieldInfo = typeof(PathUnit.Position).GetField("m_offset", BindingFlags.Public | BindingFlags.Instance);
            if (m_offsetFieldInfo == null) {
                Debug.Log("AiReplace Getting m_offsetFieldInfo failed...");
                yield break;
            }

            var mustStopAtMovableBridgeMethodInfo = typeof(CitizenGetPathTargetPositionPatch).GetMethod(nameof(MustStopAtMovableBridge), BindingFlags.NonPublic | BindingFlags.Static);
            if (mustStopAtMovableBridgeMethodInfo == null) {
                Debug.Log("AiReplace Getting mustStopAtMovableBridgeMethodInfo failed...");
                yield break;
            }

            //position.m_segment (prevSegmentID)
            yield return new CodeInstruction(OpCodes.Ldloca_S, 4);
            yield return new CodeInstruction(OpCodes.Ldfld, m_segmentFieldInfo);

            //position6.m_segment (nextSegmentID)
            yield return new CodeInstruction(OpCodes.Ldloca_S, 28);
            yield return new CodeInstruction(OpCodes.Ldfld, m_segmentFieldInfo);

            //position.m_offset (prevOffset)
            yield return new CodeInstruction(OpCodes.Ldloca_S, 4);
            yield return new CodeInstruction(OpCodes.Ldfld, m_offsetFieldInfo);

            //offset (nextOffset)
            yield return new CodeInstruction(OpCodes.Ldloc, 48);

            //MustStopAtMovableBridge(position.m_segment, position6.m_segment, position.m_offset, offset)
            yield return new CodeInstruction(OpCodes.Call, mustStopAtMovableBridgeMethodInfo);

            //if(...) goto stopAtRedPedestrianLightLabel;
            yield return new CodeInstruction(OpCodes.Brtrue, stopAtRedPedestrianLightLabel);
        }

        private static bool MustStopAtMovableBridge(ushort prevSegmentID, ushort nextSegmentID, byte prevOffset, byte nextOffset) {
            var netManager = NetManager.instance;

            ushort prevTargetNodeId = (prevOffset >= 128) ? netManager.m_segments.m_buffer[prevSegmentID].m_endNode : netManager.m_segments.m_buffer[prevSegmentID].m_startNode;
            ushort nextSourceNodeId = (nextOffset >= 128) ? netManager.m_segments.m_buffer[nextSegmentID].m_endNode : netManager.m_segments.m_buffer[nextSegmentID].m_startNode;

            if (prevTargetNodeId == nextSourceNodeId) {
                NetNode.Flags flags = netManager.m_nodes.m_buffer[prevTargetNodeId].m_flags;
                if (flags.IsFlagSet(NetNode.Flags.CustomTrafficLights)) {
                    var previousSegmentAI = netManager.m_segments.m_buffer[prevSegmentID].Info.m_netAI;
                    if (!(previousSegmentAI is MovableBridgeRoadAI)) {
                        return true;
                    }
                    var nextSegmentAI = netManager.m_segments.m_buffer[nextSegmentID].Info.m_netAI;
                    if (!(nextSegmentAI is MovableBridgeRoadAI)) {
                        return true;
                    }

                    uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                    uint num6n = (uint)(prevTargetNodeId << 8) / 32768u;
                    RoadBaseAI.GetTrafficLightState(prevTargetNodeId, ref netManager.m_segments.m_buffer[prevSegmentID], currentFrameIndex - num6n, out RoadBaseAI.TrafficLightState vehicleLightState, out RoadBaseAI.TrafficLightState pedestrianLightState, out bool vehicles, out bool pedestrians);

                    //Debug.Log($"CheckSegmentChange on bridge! state: ${vehicleLightState}");
                    if (vehicleLightState == RoadBaseAI.TrafficLightState.Red) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}