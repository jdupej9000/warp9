﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public enum NormalsAlgorithm
    {
        Fast,
        FastRobust
    };

    public static class MeshNormals
    {
        public static Mesh MakeNormals(Mesh m, NormalsAlgorithm algo = NormalsAlgorithm.FastRobust)
        {
            return MakeNormals(m, m, algo).ToMesh();
        }

        public static MeshBuilder MakeNormals(PointCloud? pcl, Mesh m, NormalsAlgorithm algo = NormalsAlgorithm.FastRobust)
        {
            if (pcl is null)
                return new MeshBuilder();

            MeshView? pos = pcl.GetView(MeshViewKind.Pos3f);
            if (pos is null || !pos.AsTypedData(out ReadOnlySpan<Vector3> posData))
                throw new InvalidOperationException();

            if (!m.TryGetIndexData(out ReadOnlySpan<FaceIndices> faces))
                throw new InvalidOperationException();

            MeshBuilder mb = pcl.ToBuilder();
            int nv = pcl.VertexCount;

            List<Vector3> normalsSeg = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Normal);
            CollectionsMarshal.SetCount(normalsSeg, nv);

            switch (algo)
            {
                case NormalsAlgorithm.Fast:
                    MakeNormalsFast(CollectionsMarshal.AsSpan(normalsSeg), posData, faces);
                    break;

                case NormalsAlgorithm.FastRobust:
                    MakeNormalsFastRobust(CollectionsMarshal.AsSpan(normalsSeg), posData, faces);
                    break;
            }
           
            return mb;
        }

        public static void MakeNormalsFast(Span<Vector3> normal, ReadOnlySpan<Vector3> pos, ReadOnlySpan<FaceIndices> faces)
        {
            int nv = pos.Length;
            int nt = faces.Length;
          
            for (int i = 0; i < nv; i++)
                normal[i] = Vector3.Zero;

            for (int i = 0; i < nt; i++)
            {
                FaceIndices f = faces[i];
                Vector3 a = pos[f.I0];
                Vector3 b = pos[f.I1];
                Vector3 c = pos[f.I2];
                Vector3 n = Vector3.Cross(b - a, c - a);

                normal[f.I0] += n;
                normal[f.I1] += n;
                normal[f.I2] += n;
            }

            for (int i = 0; i < nv; i++)
                normal[i] = Vector3.Normalize(normal[i]);
        }

        public static void MakeNormalsFastRobust(Span<Vector3> normal, ReadOnlySpan<Vector3> pos, ReadOnlySpan<FaceIndices> faces)
        {
            int nv = pos.Length;
            int nt = faces.Length;

            for (int i = 0; i < nv; i++)
                normal[i] = Vector3.Zero;

            const float collapsedThresh = 1e-10f;

            for (int i = 0; i < nt; i++)
            {
                FaceIndices f = faces[i];
                Vector3 a = pos[f.I0];
                Vector3 b = pos[f.I1];
                Vector3 c = pos[f.I2];

                if ((b - a).LengthSquared() < collapsedThresh ||
                    (c - a).LengthSquared() < collapsedThresh ||
                    (c - b).LengthSquared() < collapsedThresh)
                    continue;

                Vector3 n = Vector3.Cross(b - a, c - a);

                normal[f.I0] += n;
                normal[f.I1] += n;
                normal[f.I2] += n;
            }

            for (int i = 0; i < nv; i++)
            {
                if (normal[i] == Vector3.Zero)
                    normal[i] = Vector3.Normalize(pos[i]);
                else
                    normal[i] = Vector3.Normalize(normal[i]);
            }
        }
    }
}
