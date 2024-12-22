﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Native;

namespace Warp9.Processing
{
    public static class MeshVertexSharing
    {
        public static IEnumerable<FaceIndices> EnumerateFaces(Mesh m)
        {
            if (m.IsIndexed)
            {
                MeshView? idxView = m.GetView(MeshViewKind.Indices3i, false);
                if (idxView is null || !idxView.AsTypedData(out ReadOnlySpan<FaceIndices> indices))
                    throw new InvalidOperationException();

                int ni = indices.Length;

                for (int i = 0; i < ni; i++)
                {
                    // this it to work around error CS4007: Instance of type 'System.ReadOnlySpan<Warp9.Data.FaceIndices>' cannot be preserved across 'await' or 'yield' boundary.
                    idxView.AsTypedData(out ReadOnlySpan<FaceIndices> indices2);
                    yield return indices2[i];
                }
            }
            else
            {
                int nt = m.VertexCount / 3;
                for (int i = 0; i <= nt; i++)
                    yield return new FaceIndices(3 * i, 3 * i + 1, 3 * i + 2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long PosHash(Vector3 x, Vector3 x0, Vector3 sc)
        {
            Vector3 xi = (x - x0) * sc * new Vector3(1048576, 1048576, 1048576);
            return (long)xi.X | ((long)xi.Y) << 20 | ((long)xi.Z << 40);
        }

        public static Mesh ShareVerticesByPosition(Mesh m)
        {
            PclStat3 stat = RigidTransform.MakePclStats(m);
            Vector3 x0 = stat.x0;
            Vector3 sc = Vector3.One / (stat.x1 - stat.x0);

            MeshView? posView = m.GetView(MeshViewKind.Pos3f, false);
            if (posView is null || !posView.AsTypedData(out ReadOnlySpan<Vector3> pos))
                throw new InvalidOperationException();

            MeshBuilder mb = new MeshBuilder();
            List<Vector3> newPos = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            List<FaceIndices> newIdx = mb.GetIndexSegmentForEditing<FaceIndices>();

            int vert = 0;
            Dictionary<long, int> sharedVertices = new Dictionary<long, int>();
            foreach (FaceIndices fi in EnumerateFaces(m))
            {
                long h0 = PosHash(pos[fi.I0], x0, sc);
                if (!sharedVertices.TryGetValue(h0, out int i0))
                {
                    i0 = vert++;
                    sharedVertices.Add(h0, i0);
                    newPos.Add(pos[fi.I0]);
                }

                long h1 = PosHash(pos[fi.I1], x0, sc);
                if (!sharedVertices.TryGetValue(h1, out int i1))
                {
                    i1 = vert++;
                    sharedVertices.Add(h1, i1);
                    newPos.Add(pos[fi.I1]);
                }

                long h2 = PosHash(pos[fi.I2], x0, sc);
                if (!sharedVertices.TryGetValue(h2, out int i2))
                {
                    i2 = vert++;
                    sharedVertices.Add(h2, i2);
                    newPos.Add(pos[fi.I2]);
                }

                newIdx.Add(new FaceIndices(i0, i1, i2));
            }

            return mb.ToMesh();
        }
    }
}