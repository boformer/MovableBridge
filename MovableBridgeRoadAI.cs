using ColossalFramework;
using UnityEngine;

namespace MovableBridge {
    public class MovableBridgeRoadAI : RoadBridgeAI {
        public override void UpdateNodeFlags(ushort nodeID, ref NetNode data) {
            //Debug.Log($"UpdateNodeFlags called on node {nodeID} {data.Info.name}");
            base.UpdateNodeFlags(nodeID, ref data);

            NetManager netManager = NetManager.instance;
            int segmentCount = 0;
            int bridgeSegmentCount = 0;
            for (int s = 0; s < 8; s++) {
                ushort segment = data.GetSegment(s);
                if (segment != 0) {
                    segmentCount++;
                    NetInfo segmentInfo = netManager.m_segments.m_buffer[segment].Info;
                    if (segmentInfo != null && segmentInfo.m_netAI is MovableBridgeRoadAI) {
                        bridgeSegmentCount++;
                    }
                }
            }
            if (segmentCount == 2 && bridgeSegmentCount == 2) {
                data.m_flags |= NetNode.Flags.CustomTrafficLights | NetNode.Flags.LevelCrossing | NetNode.Flags.TrafficLights;
            }
        }

        public override void SimulationStep(ushort nodeID, ref NetNode data) {
            bool trafficLights = data.m_flags.IsFlagSet(NetNode.Flags.TrafficLights);
            if (trafficLights) {
                TrafficLightSimulationStep(nodeID, ref data);
                data.m_flags &= ~NetNode.Flags.TrafficLights;
            }

            base.SimulationStep(nodeID, ref data);

            if (trafficLights) {
                data.m_flags |= NetNode.Flags.TrafficLights;
            }
        }

        public new static void TrafficLightSimulationStep(ushort nodeID, ref NetNode data) {
            NetManager netManager = Singleton<NetManager>.instance;
            uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;

            bool green = (data.m_finalCounter == 0);

            for (int i = 0; i < 8; ++i) {
                ushort segmentID = data.GetSegment(i);

                bool segmentGreen = green;
                if (!segmentGreen) {
                    NetSegment segmentData = netManager.m_segments.m_buffer[segmentID];
                    ushort otherNodeID = segmentData.m_startNode != nodeID ? segmentData.m_startNode : segmentData.m_endNode;
                    NetNode otherNodeData = netManager.m_nodes.m_buffer[otherNodeID];
                    if (otherNodeData.m_flags.IsFlagSet(NetNode.Flags.CustomTrafficLights)) {
                        segmentGreen = true;
                    }
                }

                GetTrafficLightState(nodeID, ref netManager.m_segments.m_buffer[segmentID], currentFrameIndex - 256, out TrafficLightState vehicleLightState, out TrafficLightState _, out _, out _);

                vehicleLightState &= ~TrafficLightState.IsChanging;

                if (!segmentGreen) {
                    if ((vehicleLightState & TrafficLightState.Red) == 0) {
                        vehicleLightState = TrafficLightState.GreenToRed;
                    }
                } else {
                    if ((vehicleLightState & TrafficLightState.Red) != 0) {
                        vehicleLightState = TrafficLightState.RedToGreen;
                    }
                }
                SetTrafficLightState(nodeID, ref netManager.m_segments.m_buffer[segmentID], currentFrameIndex, vehicleLightState, TrafficLightState.Red, false, false);
            }
        }
    }

}
