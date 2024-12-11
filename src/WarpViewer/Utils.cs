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
    }
}
