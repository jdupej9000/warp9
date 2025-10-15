using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class MeshBlend
    {
        public static void Add(Span<Vector3> dest, ReadOnlySpan<Vector3> src, float factor = 1)
        {
            if(dest.Length != src.Length)
                throw new InvalidOperationException();

            int n = dest.Length;
            for (int i = 0; i < n; i++)
                dest[i] += factor * src[i];
        }

        public static void Scale(Span<Vector3> dest, float factor = 1)
        {
            for(int i = 0; i < dest.Length; i++)
                dest[i] *= factor;
        }

        public static PointCloud? Mean(IEnumerable<PointCloud?> pcls)
        {
            return WeightedMean(pcls.Select((m) => (m, 1.0f)));
        }

        public static PointCloud? WeightedMean(IEnumerable<(PointCloud?, float)> pcls)
        {
            byte[] meanPos = Array.Empty<byte>();
            
            int n = 0;
            foreach (var pcl in pcls)
            {
                if (pcl.Item1 is null) continue;

                if (n == 0)
                    meanPos = new byte[pcl.Item1.VertexCount * 3 * 4];
                else if (3 * 4 * pcl.Item1.VertexCount != meanPos.Length)
                    throw new InvalidOperationException();

                pcl.Item1.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> d);
                Add(MemoryMarshal.Cast<byte, Vector3>(meanPos.AsSpan()), d, pcl.Item2);

                n++;
            }

            if (n == 0)
                return null;

            Scale(MemoryMarshal.Cast<byte, Vector3>(meanPos.AsSpan()), 1.0f / n);

            return PointCloud.FromRawPositions(meanPos.Length / 12, meanPos); 
        }
    }
}
