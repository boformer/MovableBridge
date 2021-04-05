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
        public const ushort STATE_BRIDGE_OPEN = 0b_0011_0000_0000;
        public const ushort STATE_BRIDGE_WAITING_FOR_CLOSE = 0b_0100_0000_0000;
        public const ushort STATE_BRIDGE_CLOSING = 0b_0101_0000_0000;

        public const ushort FLAG_SHIP_NEAR_BRIDGE = 0b_0001_0000_0000_0000;
        public const ushort FLAG_SHIP_PASSING_BRIDGE = 0b_0010_0000_0000_0000;

        private const byte kMinClosedTicks = 4;

        public float m_bridgeClearance = 4f;

        public static ushort GetBridgeState(ref Building data) {
            return (ushort)(data.m_customBuffer1 & STATE_MASK);
        }

        public override void GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, float elevation) {
            mode = InfoManager.InfoMode.Transport;
            subMode = InfoManager.SubInfoMode.Default;
        }

        public override string GetLocalizedStats(ushort buildingID, ref Building data) {
            byte timer = (byte)(data.m_customBuffer1 & TIMER_MASK);
            ushort state = GetBridgeState(ref data);
            string formattedState = FormatState(state);
            bool shipNearBridge = (data.m_customBuffer1 & FLAG_SHIP_NEAR_BRIDGE) != 0;
            bool shipPassingBridge = (data.m_customBuffer1 & FLAG_SHIP_PASSING_BRIDGE) != 0;
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
                case STATE_BRIDGE_OPEN:
                    return "Bridge is open";
                case STATE_BRIDGE_WAITING_FOR_CLOSE:
                    return "Waiting for ships to pass through";
                case STATE_BRIDGE_CLOSING:
                    return "Bridge is closing";
                default:
                    return "";
            }
        }

        public override void CreateBuilding(ushort buildingID, ref Building data) {
            base.CreateBuilding(buildingID, ref data);
            UpdateTrafficLights(buildingID, ref data, true);
        }

        public override void SimulationStep(ushort buildingID, ref Building data) {
            byte timer = (byte)(data.m_customBuffer1 & TIMER_MASK);
            ushort state = GetBridgeState(ref data);
            bool shipNearBridge = (data.m_customBuffer1 & FLAG_SHIP_NEAR_BRIDGE) != 0;
            bool shipPassingBridge = (data.m_customBuffer1 & FLAG_SHIP_PASSING_BRIDGE) != 0;
            if (state == STATE_BRIDGE_CLOSED) {
                if (timer < byte.MaxValue) timer++;

                if (shipNearBridge && timer > kMinClosedTicks) {
                    Debug.Log("closed, ship near bridge, timer greater 2, preparing to open bridge");
                    state = STATE_BRIDGE_WAITING_FOR_OPEN;
                    timer = 0;
                }
            } else if (state == STATE_BRIDGE_WAITING_FOR_OPEN) {
                timer++;
                Debug.Log("waiting for open, increasing timer");

                if (timer > 1) {
                    Debug.Log("waiting for open, timer greater 1");

                    if (shipNearBridge) {
                        Debug.Log("ship near bridge, opening bridge");
                        state = STATE_BRIDGE_OPENING;
                        timer = 0;
                    } else {
                        Debug.Log("no ship near bridge, back to closed state");
                        state = STATE_BRIDGE_CLOSED;
                        timer = 0;
                    }
                }
            } else if (state == STATE_BRIDGE_OPENING) {
                timer++;
                Debug.Log("opening, increasing timer");

                // TODO instead, wait for animation to finish (check Animator)

                if (timer > 0) {
                    Debug.Log("opening, timer greater 0, bridge is open");
                    state = STATE_BRIDGE_OPEN;
                    timer = 0;
                }

            } else if (state == STATE_BRIDGE_OPEN) {
                timer++;
                Debug.Log("open, increasing timer");

                if (timer > 0) {
                    if (shipPassingBridge) {
                        Debug.Log("open, ships still passing, waiting for close");
                        state = STATE_BRIDGE_WAITING_FOR_CLOSE;
                        timer = 0;
                    } else {
                        Debug.Log("open, no ships passing bridge, closing");
                        state = STATE_BRIDGE_CLOSING;
                        timer = 0;
                    }
                }
            } else if (state == STATE_BRIDGE_WAITING_FOR_CLOSE) {
                Debug.Log("waiting for close");

                if (!shipPassingBridge) {
                    Debug.Log("waiting for close, no ships passing bridge, closing");
                    state = STATE_BRIDGE_CLOSING;
                    timer = 0;
                } else {
                    Debug.Log("waiting for close, ships still passing bridge");
                }
            } else if (state == STATE_BRIDGE_CLOSING) {
                timer++;
                Debug.Log("closing, increasing timer");

                if (timer > 0) {
                    Debug.Log("closing, timer greater 0, bridge is closed");
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
            } else if (state == STATE_BRIDGE_OPENING || state == STATE_BRIDGE_OPEN || state == STATE_BRIDGE_WAITING_FOR_CLOSE) {
                animationState = 2;
            } else if (state == STATE_BRIDGE_CLOSING) {
                animationState = 1;
            } else if (state == STATE_BRIDGE_CLOSED) {
                animationState = 0;
            }
            m_info.SetRenderParameters(position, rotation, buildingState, objectIndex, animationState, color);
        }

        // TODO
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
