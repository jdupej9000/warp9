using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Warp9
{
    public static class Utils
    {
        public static void Decompose(this Quaternion q, out Vector3 axis, out float angleDegrees)
        {
            Vector3 a = new Vector3(q.X, q.Y, q.Z);
            axis = Vector3.Normalize(a);

            float angle = 2.0f * MathF.Atan2(a.Length(), q.W);
            angleDegrees = 180.0f * angle / MathF.PI;
        }

        public static float FrobeniusNorm(this Matrix4x4 m)
        {
            return m.M11 * m.M11 + m.M12 * m.M12 + m.M13 * m.M13 + m.M14 * m.M14 +
                m.M21 * m.M21 + m.M22 * m.M22 + m.M23 * m.M23 + m.M24 * m.M24 +
                m.M31 * m.M31 + m.M32 * m.M32 + m.M33 * m.M33 + m.M34 * m.M34 +
                m.M41 * m.M41 + m.M32 * m.M42 + m.M43 * m.M43 + m.M44 * m.M44;
        }
    }
}
