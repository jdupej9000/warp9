using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Utils;

namespace Warp9.Processing
{
    public class MeshAdjacency
    {
        private MeshAdjacency(int nv, int nt, int[] ptr, int[] triIdx)
        {
            triIdxPtr = ptr;
            triIdxList = triIdx;
            NumVertices = nv;
            NumFaces = nt;
        }

        int[] triIdxPtr;
        int[] triIdxList;

        public int NumVertices { get; }
        public int NumFaces { get; }

        public ReadOnlySpan<int> GetAdjacentFaceIndices(int vertIdx)
        {
            if (vertIdx < 0 || vertIdx >= NumVertices)
                return ReadOnlySpan<int>.Empty;

            return triIdxList.AsSpan(triIdxPtr[vertIdx], triIdxPtr[vertIdx + 1] - triIdxPtr[vertIdx]);
        }

        // This does not zero the val array, nor does it check its size.
        public static void AccumulateValence(Span<int> val, IFaceCollection faces)
        {
            foreach (FaceIndices fi in MeshUtils.EnumerateFaceIndices(faces))
            {
                val[fi.I0]++;
                val[fi.I1]++;
                val[fi.I2]++;
            }
        }

        public static MeshAdjacency Create(Mesh m)
        {
            if (!m.IsIndexed)
                throw new ArgumentException("The mesh must be indexed to compute vertex-face adjacency.");

            int nv = m.VertexCount;

            // === PASS 1 - Compute valence of vertices
            int[] valence = new int[nv + 1];
            AccumulateValence(valence.AsSpan(), m);
            int sumValence = MiscUtils.CumSum(valence);
            valence[nv] = sumValence;

            // === PASS 2 - Record adjacency information
            int[] ptr = new int[nv];
            int[] adj = new int[sumValence];
            int idx = 0;
            foreach (FaceIndices fi in MeshUtils.EnumerateFaceIndices(m))
            {
                adj[valence[fi.I0] + ptr[fi.I0]] = idx;
                ptr[fi.I0]++;

                adj[valence[fi.I1] + ptr[fi.I1]] = idx;
                ptr[fi.I1]++;

                adj[valence[fi.I2] + ptr[fi.I2]] = idx;
                ptr[fi.I2]++;

                idx++;
            }

            return new MeshAdjacency(nv, m.FaceCount, valence, adj);
        }
    }
}
