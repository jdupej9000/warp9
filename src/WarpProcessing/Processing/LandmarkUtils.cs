using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class LandmarkUtils
    {
        public static float[] CalculateDispersion(PointCloud mean, IEnumerable<PointCloud> pcls)
        {
            int nv = mean.VertexCount;

            float[] ret = new float[nv];

            mean.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> meanPosByte);
            ReadOnlySpan<float> meanPos = MemoryMarshal.Cast<byte, float>(meanPosByte);

            int numMesh = 0;
            foreach (PointCloud pcl in pcls)
            {
                pcl.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> pclPosByte);
                ReadOnlySpan<float> pclPos = MemoryMarshal.Cast<byte, float>(pclPosByte);

                for (int i = 0; i < nv; i++)
                {
                    float dx = meanPos[i] - pclPos[i];
                    float dy = meanPos[i + nv] - pclPos[i + nv];
                    float dz = meanPos[i + 2 * nv] - pclPos[i + 2 * nv];
                    ret[i] += dx * dx + dy * dy + dz * dz;
                }
                numMesh++;
            }

            for (int i = 0; i < nv; i++)
                ret[i] = MathF.Sqrt(ret[i] / numMesh);

            return ret;
        }

        public static void Mirror(Span<Vector3> x)
        {
            Vector3 reflect = new Vector3(-1, 1, 1);
            for (int i = 0; i < x.Length; i++)
                x[i] = x[i] * reflect;
        }

        public static int[] ReverseBilateralLandmarkIndices(ReadOnlySpan<Vector3> pos)
        {
            int n = pos.Length;
            float bestError = float.MaxValue;
            int[] bestOrder = new int[n];
            int[] curOrder = new int[n];

            // Go through every nontrivial landmark pair. The two landmarks define a plane passing through the midpoint,
            // orthogonal to the line segment. Calculate the distances (as sum of squares) to the closest landmarks,
            // after reflecting about that plane. Use the closest landmark assignment that minimizes that error.

            for (int i = 0; i < n - 1; i++) 
            {
                for (int j = i + 1; j < n; j++)
                {
                    Vector3 normal = Vector3.Normalize(pos[j] - pos[i]);
                    float d = -Vector3.Dot(Vector3.Lerp(pos[i], pos[j], 0.5f), normal);

                    float error = 0;
                    for (int k = 0; k < n; k++)
                    {
                        (int idx, float ei) = ClosestToReflected(pos, pos[k], normal, d);
                        curOrder[k] = idx;
                        error += ei;
                    }

                    if (error < bestError)
                    {
                        bestError = error;
                        Array.Copy(curOrder, bestOrder, n);
                    }
                }
            }

            return bestOrder;
        }

        private static (int, float) ClosestToReflected(ReadOnlySpan<Vector3> pos, Vector3 pt, Vector3 normal, float d)
        {
            float q = Vector3.Dot(pt, normal) + d;
            Vector3 reflected = pt - normal * q * 2.0f;

            float bestDist = float.MaxValue;
            int bestIdx = 0;
            int n = pos.Length;
            for (int i = 0; i < n; i++)
            {
                float dist = Vector3.DistanceSquared(pos[i], reflected);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return (bestIdx, bestDist);
        }
    }
}
