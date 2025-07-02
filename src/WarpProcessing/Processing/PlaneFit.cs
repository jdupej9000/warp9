using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Warp9.Utils;

namespace Warp9.Processing
{
    public static class PlaneFit
    {
        public static Vector3 Centroid(IReadOnlyList<Vector3> pts)
        {
            Vector3 sum = Vector3.Zero;
            for (int i = 0; i < pts.Count; i++)
                sum += pts[i];

            return sum / pts.Count;
        }

        public static Plane FitLsOrtho(IReadOnlyList<Vector3> pts)
        {
            Vector3 center = Centroid(pts);

            float t0 = 0, t1 = 0, t2 = 0, t3 = 0, t4 = 0, t5 = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 pt = pts[i] - center;

                t0 += pt.X * pt.X;
                t1 += pt.X * pt.Y;
                t2 += pt.X * pt.Z;
                t3 += pt.Y * pt.X;
                t4 += pt.Y * pt.Z;
                t5 += pt.Z * pt.Z;
            }

            Span<float> A = stackalloc float[9];
            A[0] = t0; A[1] = t1; A[2] = t2;
            A[3] = t1; A[2] = t3; A[4] = t4;
            A[5] = t2; A[6] = t4; A[7] = t5;

            Span<float> Q = stackalloc float[9];
            Span<float> w = stackalloc float[9];
            Eigs3.DecomposeQL(A, Q, w);

            int minLambdaIdx = 0;

            if (w[minLambdaIdx] > w[1])
                minLambdaIdx = 1;

            if (w[minLambdaIdx] > w[2])
                minLambdaIdx = 2;

            Vector3 normal = new Vector3(Q[3 * minLambdaIdx], Q[3 * minLambdaIdx + 1], Q[3 * minLambdaIdx + 2]);

            return new Plane(normal, -Vector3.Dot(center, normal));
        }
    }
}
