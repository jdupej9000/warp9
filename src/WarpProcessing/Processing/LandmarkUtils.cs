using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class LandmarkUtils
    {
        public static float[] CalculateDispersion(PointCloud mean, IEnumerable<PointCloud> pcls)
        {
            int nv = mean.VertexCount;

            float[] ret = new float[nv];

            mean.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> meanPosByte);
            ReadOnlySpan<float> meanPos = MemoryMarshal.Cast<byte, float>(meanPosByte);

            int numMesh = 0;
            foreach (PointCloud pcl in pcls)
            {
                pcl.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> pclPosByte);
                ReadOnlySpan<float> pclPos = MemoryMarshal.Cast<byte, float>(pclPosByte);

                for (int i = 0; i < nv; i++)
                {
                    float dx = meanPos[i] - pclPos[i];
                    float dy = meanPos[i + nv] - pclPos[i + nv];
                    float dz = meanPos[i + 2 * nv] - pclPos[i + 2 * nv];
                    ret[i] += dx * dx + dy * dy + dz * dz;
                }
                numMesh++;
            }

            for (int i = 0; i < nv; i++)
                ret[i] = MathF.Sqrt(ret[i] / numMesh);

            return ret;
        }
    }
}
