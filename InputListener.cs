using ICities;
using UnityEngine;

namespace MovableBridge {
    public class InputListener : ThreadingExtensionBase {
        public static bool slowDown = false;

        private bool _processed = false;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(KeyCode.M)) {
                if (!_processed) {
                    slowDown = !slowDown;
                    Debug.Log($"slowDown: ${slowDown}");
                    _processed = true;
                }
            } else {
                _processed = false;
            }
        }
    }
}
