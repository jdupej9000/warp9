using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class MeshBlend
    {
        public static void Add(Span<float> dest, ReadOnlySpan<float> src, float factor = 1)
        {
            if(dest.Length != src.Length)
                throw new InvalidOperationException();

            int n = dest.Length;
            for (int i = 0; i < n; i++)
                dest[i] += factor * src[i];
        }

        public static void Scale(Span<float> dest, float factor = 1)
        {
            for(int i = 0; i < dest.Length; i++)
                dest[i] *= factor;
        }

        public static PointCloud Mean(IEnumerable<PointCloud> pcls)
        {
            byte[] meanPos = Array.Empty<byte>();

            int n = 0;
            foreach (PointCloud pcl in pcls)
            {
                if (n == 0)
                    meanPos = new byte[pcl.VertexCount * 3 * 4];
                else if (3 * 4 * pcl.VertexCount != meanPos.Length)
                    throw new InvalidOperationException();

                pcl.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> byteData);
                Add(MemoryMarshal.Cast<byte, float>(meanPos.AsSpan()), MemoryMarshal.Cast<byte, float>(byteData), 1);

                n++;
            }

            if (n == 0)
                return PointCloud.Empty;

            Scale(MemoryMarshal.Cast<byte, float>(meanPos), 1.0f / n);

            return PointCloud.FromRawSoaPositions(meanPos.Length / 12, meanPos); 
        }
    }
}
