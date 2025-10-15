using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Native;

namespace Warp9.Processing
{
    public static class MeshFairing
    {
        public static MeshBuilder Optimize(Mesh m, float smoothingFactor = 0.5f)
        {
            if(!m.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> posData))
                throw new InvalidOperationException();

            if (!m.TryGetIndexData(out ReadOnlySpan<FaceIndices> faces))
                throw new InvalidOperationException();

            MeshBuilder mb = m.ToBuilder();
            int nv = m.VertexCount;

            List<Vector3> posSeg = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
            CollectionsMarshal.SetCount(posSeg, nv);

            Optimize(CollectionsMarshal.AsSpan(posSeg), posData, faces, smoothingFactor);

            return mb;
        }

        public static void Optimize(Span<Vector3> posOpt, ReadOnlySpan<Vector3> pos, ReadOnlySpan<FaceIndices> faces, float smoothingFactor = 0.5f)
        {
            if (posOpt.Overlaps(pos))
                throw new InvalidOperationException("Result and source position spans must not overlap.");

            int nv = pos.Length;
            MeshAdjacency adj = MeshAdjacency.Create(nv, faces);
            HashSet<int> boundary = MeshBoundary.FindBoundaryVertices(faces);

            for (int i = 0; i < nv; i++)
            {
                if (boundary.Contains(i))
                {
                    posOpt[i] = pos[i];
                }
                else
                {
                    ReadOnlySpan<int> ring0 = adj.GetRing0Vertices(i);

                    Vector3 meanPos = Vector3.Zero;
                    for (int j = 0; j < ring0.Length; j++)
                        meanPos += pos[ring0[j]];

                    meanPos /= ring0.Length;

                    posOpt[i] = (1-smoothingFactor) * pos[i] + smoothingFactor * meanPos;
                }
            }
        }
    }
}
