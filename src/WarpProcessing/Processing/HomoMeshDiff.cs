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
    public static class HomoMeshDiff
    {
        private static ReadOnlySpan<float> GetPosSoa(PointCloud pcl)
        {
            if(!pcl.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> posByte))
                return ReadOnlySpan<float>.Empty;

            return MemoryMarshal.Cast<byte, float>(posByte);
        }

        public static void VertexDistance(Span<float> result, PointCloud pcl0, PointCloud pcl1)
        {
            if (pcl0.VertexCount != pcl1.VertexCount ||
                result.Length != pcl0.VertexCount)
            {
                throw new InvalidOperationException("Point clouds not homologous or result field not of correct size.");
            }

            int nv = result.Length;
            ReadOnlySpan<float> pcl0pos = GetPosSoa(pcl0);
            ReadOnlySpan<float> pcl1pos = GetPosSoa(pcl1);

            for (int i = 0; i < nv; i++)
            {
                Vector3 x0 = new Vector3(pcl0pos[i], pcl0pos[i + nv], pcl0pos[i + 2 * nv]);
                Vector3 x1 = new Vector3(pcl1pos[i], pcl1pos[i + nv], pcl1pos[i + 2 * nv]);
                result[i] = Vector3.Distance(x0, x1);
            }
        }
    }
}
