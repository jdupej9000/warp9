using SharpDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Native
{
    public static class MeshImputation
    {
        public static PointCloud? ImputePositions(PointCloud template, PointCloud destination, ReadOnlySpan<int> allowMask, int decim=300, bool negate_mask = false)
        {
            if (!template.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> templPos, out MeshSegmentFormat templFmt) ||
                templFmt != MeshSegmentFormat.Float32x3 ||
                !destination.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> destPos, out MeshSegmentFormat destFmt) ||
                destFmt != MeshSegmentFormat.Float32x3)
            {
                return null;
            }

            int nv = template.VertexCount;
            byte[] ret = new byte[nv * 12];
            destPos.CopyTo(ret.AsSpan());

            PCL_IMPUTE_FLAGS flags = default;
            if (negate_mask) flags |= PCL_IMPUTE_FLAGS.PCL_IMPUTE_NEGATE_MASK;

            ImputeInfo info = new ImputeInfo()
            {
                d = 3,
                n = template.VertexCount,
                decim_count = decim,
                method = PCL_IMPUTE_METHOD.TPS_DECIMATED,
                flags = flags
            };

            WarpCoreStatus status = WarpCoreStatus.WCORE_OK;

            unsafe
            {
                fixed (byte* ptempl = &MemoryMarshal.GetReference(templPos))
                fixed (byte* pdest = &MemoryMarshal.GetReference(ret.AsSpan()))
                fixed (int* pallow = &MemoryMarshal.GetReference(allowMask))
                {
                    status = (WarpCoreStatus)WarpCore.pcl_impute(ref info, (nint)pdest, (nint)ptempl, (nint)pallow);
                }
            }

            return PointCloud.FromRawPositions(nv, ret);
        }
    }
}
