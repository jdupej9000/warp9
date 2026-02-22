using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class HomoMeshDiff
    {
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

                w[fi.I0] += area0;
                w[fi.I1] += area0;
                w[fi.I1] += area0;

                float metric = area1 / area0;
                if (log) metric = MathF.Log10(metric);

                if (!float.IsNormal(metric) || float.IsNaN(metric))
                    metric = 0;

                result[fi.I0] += metric;
                result[fi.I1] += metric;
                result[fi.I2] += metric;
            }

            for (int i = 0; i < nv; i++)
            {
                if(w[i] > 0) result[i] /= w[i];
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
    }
}
