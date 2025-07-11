﻿using SharpDX.D3DCompiler;
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
            if (!template.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> templPosSoa) ||
                !destination.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> destPosSoa))
            {
                return null;
            }

            int nv = template.VertexCount;
            byte[] ret = new byte[nv * 12];
            destPosSoa.CopyTo(ret.AsSpan());

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
                fixed (byte* ptempl = &MemoryMarshal.GetReference(templPosSoa))
                fixed (byte* pdest = &MemoryMarshal.GetReference(ret.AsSpan()))
                fixed (int* pallow = &MemoryMarshal.GetReference(allowMask))
                {
                    status = (WarpCoreStatus)WarpCore.pcl_impute(ref info, (nint)pdest, (nint)ptempl, (nint)pallow);
                }
            }

            return PointCloud.FromRawSoaPositions(nv, ret);
        }
    }
}
