using System;
using ColossalFramework;
using UnityEngine;

namespace MovableBridge {
    public class MovableBridgeTrainTrackAI : TrainTrackBridgeAI {
        public bool m_Movable;

        public override void UpdateNodeFlags(ushort nodeID, ref NetNode data) {
            //Debug.Log($"UpdateNodeFlags called on node {nodeID} {data.Info.name}");
            base.UpdateNodeFlags(nodeID, ref data);

            NetManager netManager = NetManager.instance;
            int segmentCount = 0;
            int movableSegmentCount = 0;
            int staticSegmentCount = 0;
            for (int s = 0; s < 8; s++) {
                ushort segment = data.GetSegment(s);
                if (segment != 0) {
                    segmentCount++;
                    NetInfo segmentInfo = netManager.m_segments.m_buffer[segment].Info;
                    if (segmentInfo != null && segmentInfo.m_netAI is MovableBridgeTrainTrackAI bridgeAI) {
                        if (bridgeAI.m_Movable)
                            movableSegmentCount++;
                        else
                            staticSegmentCount++;
                    }
                }
            }
            if (segmentCount > 1 && movableSegmentCount == 0 && staticSegmentCount == 1) {
                data.m_flags |= NetNode.Flags.CustomTrafficLights | NetNode.Flags.LevelCrossing | NetNode.Flags.TrafficLights;
            }
        }

        //public override bool IsCombatible(NetInfo with) {
        //    if (with.m_netAI is MovableBridgeTrainTrackAI bridgeAI) {
        //        return base.IsCombatible(with) && bridgeAI.m_Movable == m_Movable;
        //    }
        //    return base.IsCombatible(with);
        //}

        public override float GetNodeInfoPriority(ushort segmentID, ref NetSegment data) {
            return !m_Movable ? float.MaxValue : base.GetNodeInfoPriority(segmentID, ref data);
        }

        public override void GetNodeState(ushort nodeID, ref NetNode nodeData, ushort segmentID, ref NetSegment segmentData, out NetNode.FlagsLong flags, out Color color) {
            flags = nodeData.flags;
            color = Color.gray.gamma;

            if ((nodeData.m_flags & NetNode.Flags.TrafficLights) != 0) {
                NetManager netManager = NetManager.instance;
                for (int s = 0; s < 8; s++) {
                    ushort segment = nodeData.GetSegment(s);
                    if (segment != 0) {
                        if (netManager.m_segments.m_buffer[segment].m_infoIndex != nodeData.m_infoIndex) {
                            GetLevelCrossingNodeState(nodeID, ref nodeData, segment, ref netManager.m_segments.m_buffer[segment], ref flags, ref color);
                        }
                    }
                }
            }
        }

        public override void SimulationStep(ushort segmentID, ref NetSegment data) {
            data.m_flags &= ~NetSegment.Flags.Flooded;
            data.m_problems = Notification.RemoveProblems(data.m_problems, Notification.Problem1.Flood);
        }

        public override void SimulationStep(ushort nodeID, ref NetNode data) {
            bool trafficLights = data.m_flags.IsFlagSet(NetNode.Flags.TrafficLights) && data.m_flags.IsFlagSet(NetNode.Flags.CustomTrafficLights);
            if (trafficLights) {
                LevelCrossingSimulationStep(nodeID, ref data);
                data.m_flags &= ~NetNode.Flags.TrafficLights;
            }

            base.SimulationStep(nodeID, ref data);

            data.m_problems = Notification.RemoveProblems(data.m_problems, Notification.Problem1.Flood);

            if (trafficLights) {
                data.m_flags |= NetNode.Flags.TrafficLights;
            }
        }

        public new static void LevelCrossingSimulationStep(ushort nodeID, ref NetNode data) {
            NetManager netManager = Singleton<NetManager>.instance;
            uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;

            bool green = (data.m_finalCounter == 0);

            for (int i = 0; i < 8; ++i) {
                ushort segmentID = data.GetSegment(i);

                bool segmentGreen = green;
                if (!segmentGreen) {
                    NetSegment segmentData = netManager.m_segments.m_buffer[segmentID];
                    if (segmentData.m_infoIndex == data.m_infoIndex) {
                        segmentGreen = true;
                    }
                }

                RoadBaseAI.GetTrafficLightState(nodeID, ref netManager.m_segments.m_buffer[segmentID], currentFrameIndex - 256, out RoadBaseAI.TrafficLightState vehicleLightState, out RoadBaseAI.TrafficLightState _, out _, out _);

                vehicleLightState &= ~RoadBaseAI.TrafficLightState.IsChanging;

                if (!segmentGreen) {
                    if ((vehicleLightState & RoadBaseAI.TrafficLightState.Red) == 0) {
                        vehicleLightState = RoadBaseAI.TrafficLightState.GreenToRed;
                    }
                } else {
                    if ((vehicleLightState & RoadBaseAI.TrafficLightState.Red) != 0) {
                        vehicleLightState = RoadBaseAI.TrafficLightState.RedToGreen;
                    }
                }
                RoadBaseAI.SetTrafficLightState(nodeID, ref netManager.m_segments.m_buffer[segmentID], currentFrameIndex, vehicleLightState, RoadBaseAI.TrafficLightState.Red, false, false);
            }
        }
    }

}
