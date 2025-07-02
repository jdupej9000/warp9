using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class MeshBoundary
    {
        public static HashSet<int> FindBoundaryVertices(ReadOnlySpan<FaceIndices> indices)
        {
            Dictionary<ulong, int> edges = new Dictionary<ulong, int>();

            void _AddEdge(int i0, int i1)
            {
                ulong code = EdgeCode(i0, i1);
                if(edges.TryGetValue(code, out int num))
                    edges[code] = num + 1;
                else 
                    edges[code] = 1;
            }

            foreach (FaceIndices fi in indices)
            {
                _AddEdge(fi.I0, fi.I1);
                _AddEdge(fi.I0, fi.I2);
                _AddEdge(fi.I1, fi.I2);
            }

            HashSet<int> boundaryVertices = new HashSet<int>();
            foreach (var kvp in edges) 
            {
                if (kvp.Value == 1)
                {
                    VerticesFromEdge(kvp.Key, out int i0, out int i1);
                    boundaryVertices.Add(i0);
                    boundaryVertices.Add(i1);
                }
            }

            return boundaryVertices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong EdgeCode(int i0, int i1)
        {
            if (i0 < i1) return ((ulong)i1) | (((ulong)i0) << 32);
            else return ((ulong)i0) | (((ulong)i1) << 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void VerticesFromEdge(ulong e, out int i0, out int i1)
        {
            i0 = (int)(e & 0xffffffffu);
            i1 = (int)((e >> 32) & 0xffffffffu);
        }
    }
}
