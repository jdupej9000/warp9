using SharpDX.DXGI;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Utils;

namespace Warp9.Processing
{
    public static class HomoMeshDiff
    {
        public static void PerVertexTwoSamples(Span<float> result, PointCloud[] sample1, PointCloud[] sample2, PointCloud proj, Func<int, ReadOnlySpan<Vector3>, ReadOnlySpan<Vector3>, ReadOnlySpan<Vector3>, ReadOnlySpan<Vector3>, float> fun)
        {
            const int BlockSize = 16;

            int k = result.Length;
            int n1 = sample1.Length;
            int n2 = sample2.Length;
            
            Vector3[] tile1 = new Vector3[n1 * BlockSize];
            Vector3[] tile2 = new Vector3[n2 * BlockSize];
            Vector3[] tileProj = new Vector3[BlockSize];
            Vector3[] tileNormal = new Vector3[BlockSize];

            for (int i = 0; i < k; i += BlockSize)
            {
                int blk = Math.Min(BlockSize, k - i);

                TryCopySegmentPart(proj, MeshSegmentSemantic.Position, i, blk, tileProj.AsSpan(), 0, 1);
                TryCopySegmentPart(proj, MeshSegmentSemantic.Normal, i, blk, tileNormal.AsSpan(), 0, 1);

                // TODO: flip to staggered reads and sequential writes
                for (int j = 0; j < n1; i++)
                    TryCopySegmentPart(sample1[j], MeshSegmentSemantic.Position, i, blk, tile1.AsSpan(), j, n1);

                for (int j = 0; j < n2; i++)
                    TryCopySegmentPart(sample2[j], MeshSegmentSemantic.Position, i, blk, tile2.AsSpan(), j, n2);

                for (int j = 0; j < blk; j++)
                    result[j + i] = fun(j + i, tile1.AsSpan(j * n1, n1), tile2.AsSpan(j * n2, n2), tileProj.AsSpan(), tileNormal.AsSpan());
            }
        }

        // Uncorrected p-values of t-tests on the normalwise distances to a common surface.
        public static void SignedSurfaceDistanceWelchTest(Span<float> result, Mesh projectionMesh, PointCloud[] sample1, PointCloud[] sample2)
        {
            int n1 = sample1.Length;
            int n2 = sample2.Length;

            float[] xa = new float[n1];
            float[] xb = new float[n2];

            PerVertexTwoSamples(result, sample1, sample2, projectionMesh,
                (idx, pts1, pts2, pos, norm) =>
                {
                    for (int i = 0; i < n1; i++)
                        xa[i] = Vector3.Dot(pts1[i] - pos[i], norm[i]);

                    for (int i = 0; i < n2; i++)
                        xb[i] = Vector3.Dot(pts2[i] - pos[i], norm[i]);

                    return (float)MathUtils.WelchTest(xa.AsSpan(), xb.AsSpan());
                });
        }

        // Uncorrected p-values of t-tests on the Euclidean distances (with sign wrt. normal) to a common surface.
        public static void SignedVertexDistanceWelchTest(Span<float> result, Mesh projectionMesh, PointCloud[] sample1, PointCloud[] sample2)
        {
            int n1 = sample1.Length;
            int n2 = sample2.Length;

            float[] xa = new float[n1];
            float[] xb = new float[n2];

            PerVertexTwoSamples(result, sample1, sample2, projectionMesh,
                (idx, pts1, pts2, pos, norm) =>
                {
                    for (int i = 0; i < n1; i++)
                        xa[i] = (pts1[i] - pos[i]).Length() * MathF.Sign(Vector3.Dot(pts1[i] - pos[i], norm[i]));

                    for (int i = 0; i < n2; i++)
                        xb[i] = (pts2[i] - pos[i]).Length() * MathF.Sign(Vector3.Dot(pts2[i] - pos[i], norm[i]));

                    return (float)MathUtils.WelchTest(xa.AsSpan(), xb.AsSpan());
                });
        }

        private static bool TryCopySegmentPart<T>(PointCloud pcl, MeshSegmentSemantic sem, int start, int num, Span<T> dest, int outOffset, int outStride) where T : struct
        {
            if (!pcl.TryGetData<T>(sem, out BufferSegment<T> seg))
                return false;

            ReadOnlySpan<T> src = seg.Data.Slice(start, num);
            for (int i = 0; i < num; i++)
                dest[i * outStride + outOffset] = src[i];

            return true;
        }

        public static void FaceScalingFactor(Span<float> result, IFaceCollection faces, PointCloud pcl0, PointCloud pcl1, bool log=false)
        {
            if (pcl0.VertexCount != pcl1.VertexCount ||
                result.Length < pcl0.VertexCount)
                throw new InvalidOperationException();

            if (!pcl0.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos0) ||
                !pcl1.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos1))
            {
                throw new InvalidOperationException("Cannot extract the position fields.");
            }

            if (!faces.TryGetIndexData(out ReadOnlySpan<FaceIndices> indices))
                throw new InvalidOperationException();

            int nv = pcl0.VertexCount;
            float[] w = ArrayPool<float>.Shared.Rent(nv);

            
            for (int i = 0; i < nv; i++)
                result[i] = 0;

            int nt = indices.Length;
            for (int i = 0; i < nt; i++)
            {
                FaceIndices fi = indices[i];
                float area0 = MeshUtils.TriangleAreaCross(pos0[fi.I0], pos0[fi.I1], pos0[fi.I2]);
                float area1 = MeshUtils.TriangleAreaCross(pos1[fi.I0], pos1[fi.I1], pos1[fi.I2]);

                float weight = 1;
                float metric = area1 / area0;

                if (!float.IsNormal(metric) || float.IsNaN(metric))
                {
                    metric = 0;
                    weight = 0;
                }
                else if (log)
                {
                    metric = MathF.Log10(metric);
                }

                result[fi.I0] += metric;
                result[fi.I1] += metric;
                result[fi.I2] += metric;

                w[fi.I0] += weight;
                w[fi.I1] += weight;
                w[fi.I2] += weight;
            }

            for (int i = 0; i < nv; i++)
            {
                if(w[i] > 0) result[i] /= w[i];
                else result[i] = 0;
            }

            ArrayPool<float>.Shared.Return(w);
        }

        public static void VertexDistance(Span<float> result, PointCloud pcl0, PointCloud pcl1)
        {
            if (pcl0.VertexCount != pcl1.VertexCount ||
                result.Length != pcl0.VertexCount)
            {
                throw new InvalidOperationException("Point clouds not homologous or result field not of correct size.");
            }

            int nv = result.Length;
            if (!pcl0.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos0) ||
                !pcl1.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos1))
            {
                throw new InvalidOperationException("Cannot extract the position fields.");
            }
        
            for (int i = 0; i < nv; i++)
                result[i] = Vector3.Distance(pos0[i], pos1[i]);
        }

        public static void SignedVertexDistance(Span<float> result, PointCloud pcl0, PointCloud pcl1)
        {
            if (pcl0.VertexCount != pcl1.VertexCount ||
                result.Length != pcl0.VertexCount)
            {
                throw new InvalidOperationException("Point clouds not homologous or result field not of correct size.");
            }

            int nv = result.Length;
            if (!pcl0.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos0) ||
               !pcl1.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos1) ||
               !pcl1.TryGetData(MeshSegmentSemantic.Normal, out ReadOnlySpan<Vector3> normal))
            {
                throw new InvalidOperationException("Cannot extract the position or normal fields.");
            }
         
            if (normal.Length != pos0.Length)
                throw new ArgumentException("The second point cloud does not have vertex normals.");

            for (int i = 0; i < nv; i++)
            {
                float f = 1;
                if (Vector3.Dot(pos1[i] - pos0[i], normal[i]) < 0) 
                    f = -1;

                result[i] = f * Vector3.Distance(pos0[i], pos1[i]);
            }
        }

        public static void SignedSurfaceDistance(Span<float> result, PointCloud pcl0, PointCloud pcl1)
        {
            if (pcl0.VertexCount != pcl1.VertexCount ||
                result.Length != pcl0.VertexCount)
            {
                throw new InvalidOperationException("Point clouds not homologous or result field not of correct size.");
            }

            int nv = result.Length;
            if (!pcl0.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos0) ||
               !pcl1.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos1) ||
               !pcl1.TryGetData(MeshSegmentSemantic.Normal, out ReadOnlySpan<Vector3> normal))
            {
                throw new InvalidOperationException("Cannot extract the position or normal fields.");
            }

            if (normal.Length != pos0.Length)
                throw new ArgumentException("The second point cloud does not have vertex normals.");

            for (int i = 0; i < nv; i++)
            {
                result[i] = Vector3.Dot(pos1[i] - pos0[i], normal[i]);
            }
        }

        public static void SurfaceDistance(Span<float> result, PointCloud pcl0, PointCloud pcl1)
        {
            if (pcl0.VertexCount != pcl1.VertexCount ||
                result.Length != pcl0.VertexCount)
            {
                throw new InvalidOperationException("Point clouds not homologous or result field not of correct size.");
            }

            int nv = result.Length;
            if (!pcl0.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos0) ||
               !pcl1.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos1) ||
               !pcl1.TryGetData(MeshSegmentSemantic.Normal, out ReadOnlySpan<Vector3> normal))
            {
                throw new InvalidOperationException("Cannot extract the position or normal fields.");
            }

            if (normal.Length != pos0.Length)
                throw new ArgumentException("The second point cloud does not have vertex normals.");

            for (int i = 0; i < nv; i++)
            {
                result[i] = MathF.Abs(Vector3.Dot(pos1[i] - pos0[i], normal[i]));
            }
        }

        public static void NormalwiseDispersion(Span<float> result, Mesh mean, PointCloud[] pcls)
        {
            if(!mean.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> posMean) ||
               !mean.TryGetData(MeshSegmentSemantic.Normal, out ReadOnlySpan<Vector3> normal))
            {
                return;
            }

            for (int j = 0; j < result.Length; j++)
                result[j] = 0;

            int numMeshes = 0;
            for(int i = 0; i < pcls.Length; i++)
            {
                if (pcls[i].TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos))
                {
                    for(int j = 0; j < pos.Length; j++)
                    {
                        result[j] += MathF.Abs(Vector3.Dot(pos[j] - posMean[j], normal[j]));
                    }

                    numMeshes++;
                }
            }

            if (numMeshes == 0)
                return;

            float factor = 1.0f / numMeshes;
            for (int j = 0; j < result.Length; j++)
                result[j] *= factor;
        }

        public static void TangentialDispersion(Span<float> result, Mesh mean, PointCloud[] pcls)
        {
            if (!mean.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> posMean) ||
                !mean.TryGetData(MeshSegmentSemantic.Normal, out ReadOnlySpan<Vector3> normal))
            {
                return;
            }

            for (int j = 0; j < result.Length; j++)
                result[j] = 0;

            int numMeshes = 0;
            for (int i = 0; i < pcls.Length; i++)
            {
                if (pcls[i].TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos))
                {
                    for (int j = 0; j < pos.Length; j++)
                    {
                        float nw = Vector3.Dot(pos[j] - posMean[j], normal[j]);
                        float total2 = Vector3.DistanceSquared(pos[j], posMean[j]);
                        result[j] += MathF.Sqrt(total2 - nw * nw);
                    }

                    numMeshes++;
                }
            }

            if (numMeshes == 0)
                return;

            float factor = 1.0f / numMeshes;
            for (int j = 0; j < result.Length; j++)
                result[j] *= factor;
        }
    }
}
