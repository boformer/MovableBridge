using System;
using ColossalFramework;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace MovableBridge {
    public class MovableBridgeAI : PlayerBuildingAI {
        public const ushort TIMER_MASK = 0b_0000_1111_1111;
        public const ushort STATE_MASK = 0b_1111_0000_0000;

        public const ushort STATE_BRIDGE_CLOSED = 0b_0000_0000_0000;
        public const ushort STATE_BRIDGE_WAITING_FOR_OPEN = 0b_0001_0000_0000;
        public const ushort STATE_BRIDGE_OPENING = 0b_0010_0000_0000;
        public const ushort STATE_BRIDGE_OPEN_LEFT = 0b_0011_0000_0000;
        public const ushort STATE_BRIDGE_WAITING_LEFT = 0b_0100_0000_0000;
        public const ushort STATE_BRIDGE_OPEN_RIGHT = 0b_0101_0000_0000;
        public const ushort STATE_BRIDGE_WAITING_RIGHT = 0b_0110_0000_0000;
        public const ushort STATE_BRIDGE_OPEN_BOTH = 0b_0111_0000_0000;
        public const ushort STATE_BRIDGE_WAITING_BOTH = 0b_1000_0000_0000;
        public const ushort STATE_BRIDGE_CLOSING = 0b_1001_0000_0000;

        public const ushort FLAG_SHIP_NEAR_BRIDGE = 0b_0001_0000_0000_0000;
        public const ushort FLAG_SHIP_PASSING_BRIDGE_LEFT = 0b_0010_0000_0000_0000;
        public const ushort FLAG_SHIP_PASSING_BRIDGE_RIGHT = 0b_0100_0000_0000_0000;
        public const ushort FLAG_SHIP_PASSING_BRIDGE_ANY = FLAG_SHIP_PASSING_BRIDGE_LEFT | FLAG_SHIP_PASSING_BRIDGE_RIGHT;

        private const byte kMinClosedTicks = 4;

        [CustomizableProperty("Pre Opening Duration", "Movable Bridge")]
        public int m_PreOpeningDuration = 2;

        [CustomizableProperty("Opening Duration", "Movable Bridge")]
        public int m_OpeningDuration = 1;

        [CustomizableProperty("Closing Duration", "Movable Bridge")]
        public int m_ClosingDuration = 1;

        [CustomizableProperty("Allow Two-way ship traffic", "Movable Bridge")]
        public bool m_AllowTwoWayTraffic = false;

        [CustomizableProperty("BridgeClearance", "Movable Bridge")]
        public float m_BridgeClearance = 4f;

        public static ushort GetBridgeState(ref Building data) {
            return (ushort)(data.m_customBuffer1 & STATE_MASK);
        }

        public override void GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, float elevation) {
            mode = InfoManager.InfoMode.Transport;
            subMode = InfoManager.SubInfoMode.Default;
        }

#if DEBUG
        public override string GetLocalizedStats(ushort buildingID, ref Building data) {
            byte timer = (byte)(data.m_customBuffer1 & TIMER_MASK);
            ushort state = GetBridgeState(ref data);
            string formattedState = FormatState(state);
            bool shipNearBridge = (data.m_customBuffer1 & FLAG_SHIP_NEAR_BRIDGE) != 0;
            bool shipPassingBridge = (data.m_customBuffer1 & FLAG_SHIP_PASSING_BRIDGE_ANY) != 0;
            return formattedState + "\n" + $"timer: {timer}, shipNearBridge: {shipNearBridge}, shipPassingBridge: {shipPassingBridge}";
        }

        private static string FormatState(ushort state) {
            switch (state) {
                case STATE_BRIDGE_CLOSED:
                    return "Bridge closed";
                case STATE_BRIDGE_WAITING_FOR_OPEN:
                    return "Waiting for pedestrians and cars to leave";
                case STATE_BRIDGE_OPENING:
                    return "Bridge is opening";
                case STATE_BRIDGE_OPEN_LEFT:
                    return "Bridge is open for left side";
                case STATE_BRIDGE_WAITING_LEFT:
                    return "Waiting for ships from left side to pass through";
                case STATE_BRIDGE_OPEN_RIGHT:
                    return "Bridge is open for right side";
                case STATE_BRIDGE_WAITING_RIGHT:
                    return "Waiting for ships from right side to pass through";
                case STATE_BRIDGE_OPEN_BOTH:
                    return "Bridge is open for both sides";
                case STATE_BRIDGE_WAITING_BOTH:
                    return "Waiting for ships from both sides to pass through";
                case STATE_BRIDGE_CLOSING:
                    return "Bridge is closing";
                default:
                    return "";
            }
        }
#endif

        public override void CreateBuilding(ushort buildingID, ref Building data) {
            base.CreateBuilding(buildingID, ref data);
            UpdateTrafficLights(buildingID, ref data, true);
        }

        public override void SimulationStep(ushort buildingID, ref Building data) {
            byte timer = (byte)(data.m_customBuffer1 & TIMER_MASK);
            ushort state = GetBridgeState(ref data);
            bool shipNearBridge = (data.m_customBuffer1 & FLAG_SHIP_NEAR_BRIDGE) != 0;
            bool shipPassingBridgeLeft = (data.m_customBuffer1 & FLAG_SHIP_PASSING_BRIDGE_LEFT) != 0;
            bool shipPassingBridgeRight = (data.m_customBuffer1 & FLAG_SHIP_PASSING_BRIDGE_RIGHT) != 0;
            if (state == STATE_BRIDGE_CLOSED) {
                if (timer < byte.MaxValue) timer++;

                if (shipNearBridge && timer > kMinClosedTicks) {
                    //Debug.Log("closed, ship near bridge, timer greater 2, preparing to open bridge");
                    state = STATE_BRIDGE_WAITING_FOR_OPEN;
                    timer = 0;
                }
            } else if (state == STATE_BRIDGE_WAITING_FOR_OPEN) {
                timer++;
                //Debug.Log("waiting for open, increasing timer");

                if (timer >= m_PreOpeningDuration) {
                    //Debug.Log("waiting for open, timer greater 1");

                    if (shipNearBridge) {
                        //Debug.Log("ship near bridge, opening bridge");
                        state = STATE_BRIDGE_OPENING;
                        timer = 0;
                    } else {
                        //Debug.Log("no ship near bridge, back to closed state");
                        state = STATE_BRIDGE_CLOSED;
                        timer = 0;
                    }
                }
            } else if (state == STATE_BRIDGE_OPENING) {
                timer++;
                //Debug.Log("opening, increasing timer");

                if (timer >= m_OpeningDuration) {
                    //Debug.Log("opening, timer greater 0, bridge is open");
                    state = m_AllowTwoWayTraffic ? STATE_BRIDGE_OPEN_BOTH : STATE_BRIDGE_OPEN_LEFT;
                    timer = 0;
                }

            } else if (state == STATE_BRIDGE_OPEN_LEFT) {
                timer++;
                //Debug.Log("open left, increasing timer");

                if (timer > 0) {
                    if (shipPassingBridgeLeft) {
                        //Debug.Log("open left, ships from left side still passing, waiting for close");
                        state = STATE_BRIDGE_WAITING_LEFT;
                        timer = 0;
                    } else {
                        //Debug.Log("open left, no ships passing bridge from left side, switching to right side traffic");
                        state = STATE_BRIDGE_OPEN_RIGHT;
                        timer = 0;
                    }
                }
            } else if (state == STATE_BRIDGE_WAITING_LEFT) {
                //Debug.Log("waiting left");

                if (!shipPassingBridgeLeft) {
                    //Debug.Log("waiting left, no ships passing bridge from left side, switching to right side traffic");
                    state = STATE_BRIDGE_OPEN_RIGHT;
                    timer = 0;
                } else {
                    //Debug.Log("waiting left, ships still passing bridge");
                }
            } else if (state == STATE_BRIDGE_OPEN_RIGHT) {
                timer++;
                //Debug.Log("open right, increasing timer");

                if (timer > 0) {
                    if (shipPassingBridgeRight) {
                        //Debug.Log("open right, ships from right side still passing, waiting for close");
                        state = STATE_BRIDGE_WAITING_RIGHT;
                        timer = 0;
                    } else {
                        //Debug.Log("open right, no ships passing bridge from right side, switching to right side traffic");
                        state = STATE_BRIDGE_CLOSING;
                        timer = 0;
                    }
                }
            } else if (state == STATE_BRIDGE_WAITING_RIGHT) {
                //Debug.Log("waiting right");

                if (!shipPassingBridgeRight) {
                    //Debug.Log("waiting right, no ships passing bridge, closing");
                    state = STATE_BRIDGE_CLOSING;
                    timer = 0;
                } else {
                    //Debug.Log("waiting right, ships still passing bridge");
                }
            } else if (state == STATE_BRIDGE_OPEN_BOTH) {
                timer++;
                //Debug.Log("open both, increasing timer");

                if (timer > 0) {
                    if (shipPassingBridgeLeft || shipPassingBridgeRight) {
                        //Debug.Log("open both, ships still passing, waiting for close");
                        state = STATE_BRIDGE_WAITING_BOTH;
                        timer = 0;
                    } else {
                        //Debug.Log("open both, no ships passing bridge, closing");
                        state = STATE_BRIDGE_CLOSING;
                        timer = 0;
                    }
                }
            } else if (state == STATE_BRIDGE_WAITING_BOTH) {
                //Debug.Log("waiting both");

                if (!shipPassingBridgeLeft && !shipPassingBridgeRight) {
                    //Debug.Log("waiting both, no ships passing bridge, closing");
                    state = STATE_BRIDGE_CLOSING;
                    timer = 0;
                } else {
                    //Debug.Log("waiting right/both, ships still passing bridge");
                }
            } else if (state == STATE_BRIDGE_CLOSING) {
                timer++;
                //Debug.Log("closing, increasing timer");

                if (timer >= m_ClosingDuration) {
                    //Debug.Log("closing, timer greater 0, bridge is closed");
                    state = STATE_BRIDGE_CLOSED;
                    timer = 0;
                }
            }

            UpdateTrafficLights(buildingID, ref data, green: (state == STATE_BRIDGE_CLOSED));

            data.m_customBuffer1 = 0;
            data.m_customBuffer1 |= timer;
            data.m_customBuffer1 |= state;

            base.SimulationStep(buildingID, ref data);
        }

        public override void SetRenderParameters(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, Vector3 position, Quaternion rotation, Vector4 buildingState, Vector4 objectIndex, Color color) {
            ushort state = GetBridgeState(ref data);
            int animationState = 0;
            if (state == STATE_BRIDGE_WAITING_FOR_OPEN) {
                animationState = 1;
            } else if (state >= STATE_BRIDGE_OPENING && state <= STATE_BRIDGE_WAITING_BOTH) {
                animationState = 2;
            } else if (state == STATE_BRIDGE_CLOSING) {
                animationState = 1;
            } else if (state == STATE_BRIDGE_CLOSED) {
                animationState = 0;
            }
            m_info.SetRenderParameters(position, rotation, buildingState, objectIndex, animationState, color);
        }

        private void UpdateTrafficLights(ushort buildingID, ref Building data, bool green) {
            NetManager netManager = NetManager.instance;

            ushort nodeID = data.m_netNode;
            int counter = 0;

            while (nodeID != 0) {
                if (netManager.m_nodes.m_buffer[nodeID].m_flags.IsFlagSet(NetNode.Flags.CustomTrafficLights)) {
                    netManager.m_nodes.m_buffer[nodeID].m_finalCounter = green ? (ushort)0 : (ushort)1;
                }

                nodeID = netManager.m_nodes.m_buffer[nodeID].m_nextBuildingNode;

                if (++counter > 32768) {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }
    }
}
