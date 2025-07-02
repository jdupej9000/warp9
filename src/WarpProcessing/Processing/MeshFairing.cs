using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class MeshFairing
    {
        public static void Optimize(Span<Vector3> posOpt, ReadOnlySpan<Vector3> pos, ReadOnlySpan<FaceIndices> faces)
        {
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
                }
            }
        }
    }
}
