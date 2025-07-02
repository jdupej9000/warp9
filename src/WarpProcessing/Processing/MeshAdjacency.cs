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
        private MeshAdjacency(int nv, int nt, int[] ptr, int[] triIdx, int[] vxPtr, int[] vxIdx)
        {
            triIdxPtr = ptr;
            triIdxList = triIdx;
            vertIdxPtr = vxPtr;
            vertIdxList = vxIdx;
            NumVertices = nv;
            NumFaces = nt;
        }

        int[] triIdxPtr;
        int[] triIdxList;
        int[] vertIdxPtr;
        int[] vertIdxList;

        public int NumVertices { get; }
        public int NumFaces { get; }

        public ReadOnlySpan<int> GetAdjacentFaceIndices(int vertIdx)
        {
            if (vertIdx < 0 || vertIdx >= NumVertices)
                return ReadOnlySpan<int>.Empty;

            return triIdxList.AsSpan(triIdxPtr[vertIdx], triIdxPtr[vertIdx + 1] - triIdxPtr[vertIdx]);
        }

        public ReadOnlySpan<int> GetRing0Vertices(int vertIdx)
        {
            if (vertIdx < 0 || vertIdx >= NumVertices)
                return ReadOnlySpan<int>.Empty;

            return vertIdxList.AsSpan(vertIdxPtr[vertIdx], vertIdxPtr[vertIdx + 1] - vertIdxPtr[vertIdx]);
        }

        // This does not zero the val array, nor does it check its size.
        public static void AccumulateValence(Span<int> val, ReadOnlySpan<FaceIndices> faces)
        {
            foreach (FaceIndices fi in faces)
            {
                val[fi.I0]++;
                val[fi.I1]++;
                val[fi.I2]++;
            }
        }

        public static MeshAdjacency Create(int nv, ReadOnlySpan<FaceIndices> faces)
        {
            int nt = faces.Length;

            // === PASS 1 - Compute valence of vertices
            int[] valence = new int[nv + 1];
            AccumulateValence(valence.AsSpan(), faces);
            int sumValence = MiscUtils.CumSum(valence);
            valence[nv] = sumValence;

            // === PASS 2 - Record adjacency information
            int[] ptr = ArrayPool<int>.Shared.Rent(nv);
            for (int i = 0; i < nv; i++)
                ptr[i] = 0;

            int[] adj = new int[sumValence];
            int idx = 0;
            foreach (FaceIndices fi in faces)
            {
                adj[valence[fi.I0] + ptr[fi.I0]] = idx;
                ptr[fi.I0]++;

                adj[valence[fi.I1] + ptr[fi.I1]] = idx;
                ptr[fi.I1]++;

                adj[valence[fi.I2] + ptr[fi.I2]] = idx;
                ptr[fi.I2]++;

                idx++;
            }

            ArrayPool<int>.Shared.Return(ptr);

            // === PASS 3 - Build vertex adjacency from face adjacency
            List<int> vertAdj = new List<int>();
            int[] vertAdjPtr = new int[nv + 1];
            HashSet<int> vertAdjAccum = new HashSet<int>();
            int vertAdjIdx = 0;
            for (int i = 0; i < nv; i++)
            {
                vertAdjPtr[i] = vertAdjIdx;
                vertAdjAccum.Clear();

                for (int j = valence[i]; j < valence[i + 1]; j++)
                {
                    FaceIndices fi = faces[adj[j]];
                    if (fi.I0 != i) vertAdjAccum.Add(fi.I0);
                    if (fi.I1 != i) vertAdjAccum.Add(fi.I1);
                    if (fi.I2 != i) vertAdjAccum.Add(fi.I2);
                }

                foreach(int ringVert in vertAdjAccum)
                    vertAdj.Add(ringVert);

                vertAdjIdx += vertAdjAccum.Count;
            }
            
            vertAdjPtr[nv] = vertAdjIdx;

            return new MeshAdjacency(nv, nt, valence, adj, vertAdjPtr, vertAdj.ToArray());
        }
    }
}
