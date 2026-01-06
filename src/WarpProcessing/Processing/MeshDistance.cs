using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Warp9.Data;
using Warp9.Native;
using Warp9.Utils;

namespace Warp9.Processing
{
    public enum MeshDistanceKind
    {   
        ProcrustesRaw = 1,
        Procrustes = 2
    };

    public class MeshDistance
    {
        private static bool RequiresOpa(IEnumerable<MeshDistanceKind> kinds)
        {
            return kinds.Any((t) => t == MeshDistanceKind.Procrustes);
        }

        public static float DistanceProcrustes(PointCloud pclA, float scaleA, PointCloud pclB, float scaleB, bool[]? allow)
        {
            int n = pclA.VertexCount;
            if (n != pclB.VertexCount ||
                !pclA.TryGetData(MeshSegmentSemantic.Position, out BufferSegment<Vector3> segA) ||
                !pclB.TryGetData(MeshSegmentSemantic.Position, out BufferSegment<Vector3> segB))
            {
                throw new InvalidOperationException();
            }

            ReadOnlySpan<Vector3> ptA = segA.Data;
            ReadOnlySpan<Vector3> ptB = segB.Data;

            double rms = 0;

            if (allow is not null)
            {
                int nallow = 0;
                for (int i = 0; i < n; i++)
                {
                    if (allow[i])
                    {
                        rms += Vector3.DistanceSquared(scaleA * ptA[i], scaleB * ptB[i]);
                        nallow++;
                    }

                    rms /= nallow;
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                    rms += Vector3.DistanceSquared(scaleA * ptA[i], scaleB * ptB[i]);

                rms /= n;
            }

            return (float)Math.Sqrt(rms);
        }

        private static void ComputeDistances(Dictionary<MeshDistanceKind, Matrix<float>> res, int a, int b, bool opaHint, IReadOnlyList<PointCloud> pcls, IReadOnlyList<float>? scale, bool[]? allow, int[]? allowBitField, MeshDistanceKind[] kinds)
        {
            if (a == b)
            {
                foreach (MeshDistanceKind k in kinds)
                    res[k][a, b] = 0;
            }
            else
            {
                PointCloud pclA = pcls[a];
                PointCloud pclB = pcls[b];
                PointCloud pclBalign = pclB;
                float scaleA = 1, scaleB = 1;

                if (scale is not null)
                {
                    scaleA = scale[a];
                    scaleB = scale[b];
                }

                if (opaHint)
                {
                    Rigid3 rigid = RigidTransform.FitOpa(pclA, pclB, allowBitField); // rigid transforms pcl1 -> pcl2
                    rigid.cs = 1;
                    pclBalign = RigidTransform.TransformPosition(pclB, rigid)!;
                }

                foreach (MeshDistanceKind k in kinds)
                {
                    float metric = k switch
                    {
                        MeshDistanceKind.ProcrustesRaw => DistanceProcrustes(pclA, scaleA, pclB, scaleB, allow),
                        MeshDistanceKind.Procrustes => DistanceProcrustes(pclA, scaleA, pclBalign, scaleB, allow),

                        _ => float.NaN
                    };

                    Matrix<float> m = res[k];
                    m[a, b] = metric;
                    m[b, a] = metric;
                }
            }
        }

        public static MatrixCollection Compute(IReadOnlyList<PointCloud> pcls, IReadOnlyList<float>? scale, bool[]? allow, MeshDistanceKind[] kinds)
        {
            int ns = pcls.Count;
            int nk = kinds.Length;
            bool doOpa = RequiresOpa(kinds);

            MatrixCollection ret = new MatrixCollection();
            Dictionary<MeshDistanceKind, Matrix<float>> mats = new Dictionary<MeshDistanceKind, Matrix<float>>();

            int[]? allowBitField = null;
            if (allow is not null) allowBitField = BitMask.MakeBitMask(allow);

            foreach (MeshDistanceKind k in kinds)
            {
                Matrix<float> m = new Matrix<float>(ns, ns, float.NaN);
                ret.Add((int)k, m);
                mats[k] = m;
            }

            PairIterator pairs = new PairIterator(ns, true);
            Parallel.ForEach(pairs, (p) => ComputeDistances(mats, p.Item1, p.Item2, doOpa, pcls, scale, allow, allowBitField, kinds));

            return ret;
        }
    }
}
