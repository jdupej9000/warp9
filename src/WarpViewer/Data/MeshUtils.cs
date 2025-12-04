using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Warp9.Data
{
    public static class MeshUtils
    {
        public static Aabb? FindBoundingBox(PointCloud pcl, MeshSegmentSemantic seg)
        {
            if (!pcl.TryGetData(seg, out BufferSegment<Vector3> data) || data.Count == 0)
                return null;

            Vector3 boxMin, boxMax;
            boxMin = boxMax = data.Data[0];

            for (int i = 1; i < data.Count; i++)
            {
                boxMin = Vector3.Min(boxMin, data[i]);
                boxMax = Vector3.Max(boxMax, data[i]);
            }

            return new Aabb(boxMin, boxMax);
        }

        public static ReadOnlySpan<FaceIndices> EnumerateFaceIndices(IFaceCollection faces)
        {
            if (!faces.IsIndexed || !faces.TryGetIndexData(out ReadOnlySpan<FaceIndices> ret))
                return ReadOnlySpan<FaceIndices>.Empty;

            return ret;
        }

        public static PointCloud PermuteSegment<T>(PointCloud pcl, MeshSegmentSemantic sem, ReadOnlySpan<int> indices)
            where T : struct
        {
            MeshBuilder mb = new MeshBuilder();

            if (!pcl.TryGetData(sem, out BufferSegment<T> data))
                throw new InvalidOperationException();

            MeshSegmentBuilder<T> outSeg = mb.GetSegmentForEditing<T>(sem, false);

            int n = indices.Length;
            for (int i = 0; i < n; i++)
                outSeg.Data.Add(data.Data[indices[i]]);

            return mb.ToPointCloud();
        }

        public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            float ret = 0.0f;
            float d0 = Vector3.Distance(a, b);
            float d1 = Vector3.Distance(c, b);
            float d2 = Vector3.Distance(a, c);
            float s = 0.5f * (d0 + d1 + d2);

            ret = MathF.Sqrt(s * (s - d0) * (s - d1) * (s - d2));
            if (float.IsNaN(ret)) 
                ret = 0;

            return ret;
        }
      
    }
}
