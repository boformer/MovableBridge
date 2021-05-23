using ColossalFramework.Math;
using UnityEngine;

namespace MovableBridge {
    public static class QuadUtils {
        public static Quad2 GetSegmentQuad(Vector3 a, Vector3 b, float halfWidth) {
            Vector2 forwardDir = VectorUtils.XZ(b - a).normalized;
            Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);
            return new Quad2 {
                a = VectorUtils.XZ(a) - halfWidth * rightDir,
                b = VectorUtils.XZ(a) + halfWidth * rightDir,
                c = VectorUtils.XZ(b) + halfWidth * rightDir,
                d = VectorUtils.XZ(b) - halfWidth * rightDir
            };
        }
    }
}
