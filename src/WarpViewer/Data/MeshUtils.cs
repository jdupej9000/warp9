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

        public static Mesh MakeIndexedMesh(float[] pos, int[] ib, float posScale = 1)
        {
            MeshBuilder mb = new MeshBuilder();
            List<Vector3> vbPos = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
            for (int i = 0; i < pos.Length; i += 3)
                vbPos.Add(posScale * new Vector3(pos[i], pos[i + 1], pos[i + 2]));

            List<FaceIndices> faces = mb.GetIndexSegmentForEditing();
            for (int i = 0; i < ib.Length; i += 3)
                faces.Add(new FaceIndices(ib[i], ib[i + 1], ib[i + 2]));

            return mb.ToMesh();
        }

        public static Mesh MakeIcosahedron(float scale=1)
        {
            float[] vb = {
                -1, 0, -1.61803f,
                0, 1.61803f, -1,
                1, 0, -1.61803f,
                0, 1.61803f, 1,
                1.61803f, 1, 0,
                1, 0, 1.61803f,
                -1.61803f, 1, 0,
                -1.61803f, -1, 0,
                -1, 0, 1.61803f,
                0, -1.61803f, 1,
                1.61803f, -1, 0,
                0, -1.61803f, -1
            };

            int[] ib = { 1, 6, 3, 0, 6, 1, 3, 4, 1, 3, 6, 8, 6, 0, 7, 2, 0, 1, 4, 3, 5, 4, 2, 1, 7, 8, 6, 5, 3, 8, 0, 11, 7, 11, 0, 2, 4, 5, 10, 10, 2, 4, 8, 7, 9, 8, 9, 5, 7, 11, 9, 11, 2, 10, 10, 5, 9, 11, 10, 9 };

            return MakeIndexedMesh(vb, ib, scale);
        }
        public static Mesh MakeCubeIndexed(float scale = 1)
        {
            float[] vb = {
                1.0f, -1.0f, -1.0f,
                1.0f, -1.0f, 1.0f,
                -1.0f, -1.0f, 1.0f,
                -1.0f, -1.0f, -1.0f,
                1.0f, 1.0f, -1.0f,
                1.0f, 1.0f, 1.0f,
                -1.0f, 1.0f, 1.0f,
                -1.0f, 1.0f, -1.0f
            };

            int[] ib = {
                1,2,3,7,6,5,4,5,1,5,6,2,2,6,7,0,3,7,0,1,3,4,7,5,0,4,1,1,5,2,3,2,7,4,0,7
            };

           return MakeIndexedMesh(vb, ib, scale);
        }

    }
}
