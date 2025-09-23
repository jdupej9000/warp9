using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class MeshSymmetrize
    {

        public static PointCloud FlipPosCoord(PointCloud pcl, bool flipX, bool flipY, bool flipZ)
        {
            if(!pcl.TryGetRawData(MeshSegmentSemantic.Position, 0, out ReadOnlySpan<byte> dataRaw))
                throw new ArgumentException(nameof(pcl));

            int nv = pcl.VertexCount;
            ReadOnlySpan<float> dataFloat = MemoryMarshal.Cast<byte, float>(dataRaw);

            byte[] retPosRaw = new byte[nv * 3 * 4];
            Span<float> outFloat = MemoryMarshal.Cast<byte, float>(retPosRaw);

            Scale(outFloat.Slice(0, nv), dataFloat.Slice(0, nv), flipX ? -1 : 1);
            Scale(outFloat.Slice(nv, nv), dataFloat.Slice(nv, nv), flipY ? -1 : 1);
            Scale(outFloat.Slice(2 * nv, nv), dataFloat.Slice(2 * nv, nv), flipZ ? -1 : 1);

            return PointCloud.FromRawSoaPositions(nv, retPosRaw);
        }

        public static PointCloud MakeSymmetricRigid(PointCloud pcl, PointCloud lms)
        {
            PointCloud pclMirror = FlipPosCoord(pcl, true, false, false);
            PointCloud lmsMirror = FlipPosCoord(lms, true, false, false);
            
            throw new NotImplementedException();
        }

        private static void Scale(Span<float> to, ReadOnlySpan<float> from, float f)
        {
            int n = to.Length;

            if (n != from.Length)
                throw new ArgumentException();

            for (int i = 0; i < n; i++)
                to[i] = from[i] * f;
        }
    }
}
