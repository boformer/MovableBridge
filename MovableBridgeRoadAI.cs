using ColossalFramework;
using UnityEngine;

namespace MovableBridge {
    public class MovableBridgeRoadAI : RoadBridgeAI {
        public override void UpdateNodeFlags(ushort nodeID, ref NetNode data) {
            Debug.Log($"UpdateNodeFlags called on node {nodeID} {data.Info.name}");
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

            //Debug.Log($"Before: Traffic lights: {data.m_flags.IsFlagSet(NetNode.Flags.TrafficLights)}, custom: {data.m_flags.IsFlagSet(NetNode.Flags.CustomTrafficLights)}");
            //NetNode.Flags flags = data.m_flags;
            //uint levelBitMask = 0u;
            //int levelCount = 0;
            //NetManager netManager = Singleton<NetManager>.instance;
            //int inboundSegmentCount = 0;
            //int inboundLaneCount = 0;
            //int segmentsWithLaneCount = 0;
            //bool wantTrafficLights = WantTrafficLights();
            //bool isTransition = false;
            //int roadSegmentsCount = 0;
            //int trainSegmentsCount = 0;
            //for (int s = 0; s < 8; s++)
            //{
            //    ushort segment = data.GetSegment(s);
            //    if (segment != 0)
            //    {
            //        NetInfo segmentInfo = netManager.m_segments.m_buffer[segment].Info;
            //        if ((object)segmentInfo != null)
            //        {
            //            uint levelBit = (uint)(1 << (int)segmentInfo.m_class.m_level);
            //            if ((levelBitMask & levelBit) == 0)
            //            {
            //                levelBitMask |= levelBit;
            //                levelCount++;
            //            }
            //            if (segmentInfo.m_netAI.WantTrafficLights())
            //            {
            //                wantTrafficLights = true;
            //            }
            //            if (segmentInfo.m_vehicleTypes.IsFlagSet(VehicleInfo.VehicleType.Car) != m_info.m_vehicleTypes.IsFlagSet(VehicleInfo.VehicleType.Car))
            //            {
            //                isTransition = true;
            //            }
            //            int forwardLaneCount = 0;
            //            int backwardLaneCount = 0;
            //            netManager.m_segments.m_buffer[segment].CountLanes(segment, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, VehicleInfo.VehicleType.Car | VehicleInfo.VehicleType.Tram, ref forwardLaneCount, ref backwardLaneCount);
            //            if (netManager.m_segments.m_buffer[segment].m_endNode == nodeID)
            //            {
            //                if (forwardLaneCount != 0)
            //                {
            //                    inboundSegmentCount++;
            //                    inboundLaneCount += forwardLaneCount;
            //                }
            //            }
            //            else if (backwardLaneCount != 0)
            //            {
            //                inboundSegmentCount++;
            //                inboundLaneCount += backwardLaneCount;
            //            }
            //            if (forwardLaneCount != 0 || backwardLaneCount != 0)
            //            {
            //                segmentsWithLaneCount++;
            //            }
            //            if (segmentInfo.m_class.m_service == ItemClass.Service.Road)
            //            {
            //                roadSegmentsCount++;
            //            }
            //            else if ((segmentInfo.m_vehicleTypes & VehicleInfo.VehicleType.Train) != 0)
            //            {
            //                trainSegmentsCount++;
            //            }
            //        }
            //    }
            //}
            //if (roadSegmentsCount >= 1 && trainSegmentsCount >= 1)
            //{
            //    flags &= (NetNode.Flags.Created | NetNode.Flags.Deleted | NetNode.Flags.Original | NetNode.Flags.Disabled | NetNode.Flags.End | NetNode.Flags.Middle | NetNode.Flags.Bend | NetNode.Flags.Junction | NetNode.Flags.Moveable | NetNode.Flags.Untouchable | NetNode.Flags.Outside | NetNode.Flags.Temporary | NetNode.Flags.Double | NetNode.Flags.Fixed | NetNode.Flags.OnGround | NetNode.Flags.Ambiguous | NetNode.Flags.Water | NetNode.Flags.Sewage | NetNode.Flags.ForbidLaneConnection | NetNode.Flags.Underground | NetNode.Flags.Transition | NetNode.Flags.LevelCrossing | NetNode.Flags.OneWayOut | NetNode.Flags.TrafficLights | NetNode.Flags.OneWayIn | NetNode.Flags.Heating | NetNode.Flags.Electricity | NetNode.Flags.Collapsed | NetNode.Flags.DisableOnlyMiddle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
            //    if (roadSegmentsCount < 1 || trainSegmentsCount < 2)
            //    {
            //        flags &= ~(NetNode.Flags.LevelCrossing | NetNode.Flags.TrafficLights);
            //    }
            //    else
            //    {
            //        flags |= (NetNode.Flags.LevelCrossing | NetNode.Flags.TrafficLights);
            //    }

            //    if (levelCount < 2 && !isTransition)
            //    {
            //        flags &= ~NetNode.Flags.Transition;
            //    }
            //    else
            //    {
            //        flags |= NetNode.Flags.Transition;
            //    }
            //}
            //else
            //{
            //    flags &= ~NetNode.Flags.LevelCrossing;
            //    if (levelCount < 2 && !isTransition)
            //    {
            //        flags &= ~NetNode.Flags.Transition;
            //    }
            //    else
            //    {
            //        flags |= NetNode.Flags.Transition;
            //    }
            //    if (wantTrafficLights)
            //    {
            //        wantTrafficLights = (inboundSegmentCount > 2
            //                             || (inboundSegmentCount >= 2 && segmentsWithLaneCount >= 3 && inboundLaneCount > 6))
            //                            && flags.IsFlagSet(NetNode.Flags.Junction);
            //    }
            //    if (flags.IsFlagSet(NetNode.Flags.CustomTrafficLights))
            //    {
            //        //Debug.Log("CustomTrafficLights!");
            //        if (!CanEnableTrafficLights(nodeID, ref data))
            //        {
            //            //Debug.Log("!CanEnableTrafficLights");
            //            flags &= (NetNode.Flags.Created | NetNode.Flags.Deleted | NetNode.Flags.Original | NetNode.Flags.Disabled | NetNode.Flags.End | NetNode.Flags.Middle | NetNode.Flags.Bend | NetNode.Flags.Junction | NetNode.Flags.Moveable | NetNode.Flags.Untouchable | NetNode.Flags.Outside | NetNode.Flags.Temporary | NetNode.Flags.Double | NetNode.Flags.Fixed | NetNode.Flags.OnGround | NetNode.Flags.Ambiguous | NetNode.Flags.Water | NetNode.Flags.Sewage | NetNode.Flags.ForbidLaneConnection | NetNode.Flags.Underground | NetNode.Flags.Transition | NetNode.Flags.LevelCrossing | NetNode.Flags.OneWayOut | NetNode.Flags.OneWayIn | NetNode.Flags.Heating | NetNode.Flags.Electricity | NetNode.Flags.Collapsed | NetNode.Flags.DisableOnlyMiddle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
            //        }
            //        else if (wantTrafficLights == data.m_flags.IsFlagSet(NetNode.Flags.TrafficLights))
            //        {
            //            //Debug.Log("wantTrafficLights == TrafficLights");
            //            flags &= (NetNode.Flags.Created | NetNode.Flags.Deleted | NetNode.Flags.Original | NetNode.Flags.Disabled | NetNode.Flags.End | NetNode.Flags.Middle | NetNode.Flags.Bend | NetNode.Flags.Junction | NetNode.Flags.Moveable | NetNode.Flags.Untouchable | NetNode.Flags.Outside | NetNode.Flags.Temporary | NetNode.Flags.Double | NetNode.Flags.Fixed | NetNode.Flags.OnGround | NetNode.Flags.Ambiguous | NetNode.Flags.Water | NetNode.Flags.Sewage | NetNode.Flags.ForbidLaneConnection | NetNode.Flags.Underground | NetNode.Flags.Transition | NetNode.Flags.LevelCrossing | NetNode.Flags.OneWayOut | NetNode.Flags.TrafficLights | NetNode.Flags.OneWayIn | NetNode.Flags.Heating | NetNode.Flags.Electricity | NetNode.Flags.Collapsed | NetNode.Flags.DisableOnlyMiddle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
            //        }
            //    }
            //    else
            //    {
            //        if (!wantTrafficLights)
            //        {
            //            flags &= ~NetNode.Flags.TrafficLights;
            //        }
            //        else
            //        {
            //            flags |= NetNode.Flags.TrafficLights;
            //        }

            //    }
            //}
            //data.m_flags = flags;

            //Debug.Log($"After: Traffic lights: {data.m_flags.IsFlagSet(NetNode.Flags.TrafficLights)}, custom: {data.m_flags.IsFlagSet(NetNode.Flags.CustomTrafficLights)}");
        }

        //private bool CanEnableTrafficLights(ushort nodeID, ref NetNode data)
        //{
        //    // begin modified
        //    //if (!data.m_flags.IsFlagSet(NetNode.Flags.Junction))
        //    //{
        //    //    return false;
        //    //}
        //    bool allSegmentsHaveSameInfo = true;
        //    // end modified

        //    int roadSegmentCount = 0;
        //    int trainSegmentCount = 0;
        //    int pedestrianSegmentCount = 0;

        //    NetManager netManager = Singleton<NetManager>.instance;
        //    for (int i = 0; i < 8; i++)
        //    {
        //        ushort segment = data.GetSegment(i);
        //        if (segment != 0)
        //        {
        //            NetInfo info = netManager.m_segments.m_buffer[segment].Info;
        //            if (info.m_class.m_service == ItemClass.Service.Road)
        //            {
        //                roadSegmentCount++;
        //            }
        //            else if ((info.m_vehicleTypes & VehicleInfo.VehicleType.Train) != 0)
        //            {
        //                trainSegmentCount++;
        //            }
        //            if (info.m_hasPedestrianLanes)
        //            {
        //                pedestrianSegmentCount++;
        //            }

        //            // begin modified
        //            if (info != data.Info)
        //            {
        //                allSegmentsHaveSameInfo = false;
        //            }
        //            // end modified
        //        }
        //    }

        //    Debug.Log($"CanEnableTrafficLights: roadSegmentCount: {roadSegmentCount}, trainSegmentCount: {trainSegmentCount}, "
        //              + $"pedestrianSegmentCount: {pedestrianSegmentCount}, oneWayIn: {data.m_flags.IsFlagSet(NetNode.Flags.OneWayIn)}");

        //    // begin modified
        //    if (!data.m_flags.IsFlagSet(NetNode.Flags.Junction))
        //    {
        //        if (roadSegmentCount == 2 && allSegmentsHaveSameInfo)
        //        {
        //            return true;
        //        }

        //        return false;
        //    }
        //    // end modified

        //    if (roadSegmentCount >= 1 && trainSegmentCount >= 1)
        //    {
        //        return false;
        //    }
        //    if (!data.m_flags.IsFlagSet(NetNode.Flags.OneWayIn))
        //    {
        //        return true;
        //    }
        //    if (pedestrianSegmentCount != 0)
        //    {
        //        return true;
        //    }
        //    return false;
        //}

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
