using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Warp9.Native
{
    public class Tps3dContext : IDisposable
    {
        private Tps3dContext(nint ctx)
        {
            nativeCtx = ctx;
        }

        nint nativeCtx;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (nativeCtx != nint.Zero)
            {
                WarpCore.tps_free(nativeCtx);
                nativeCtx = nint.Zero;
            }
        }

        public void Transform(ReadOnlySpan<Vector3> x, Span<Vector3> y)
        {
            if (x.Length != y.Length)
                throw new InvalidOperationException();

            unsafe
            {
                fixed (Vector3* ptrX = &MemoryMarshal.GetReference(x))
                fixed (Vector3* ptrY = &MemoryMarshal.GetReference(y))
                {
                    WarpCore.tps_transform(nativeCtx, x.Length, (nint)ptrX, (nint)ptrY);
                }
            }
        }


        public static Tps3dContext Fit(PointCloud source, PointCloud target)
        {
            if (source.VertexCount != target.VertexCount)
                throw new InvalidOperationException();

            if (!source.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> rawSrc, out MeshSegmentFormat fmtSrc) ||
               !target.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> rawDest, out MeshSegmentFormat fmtDest) ||
                fmtSrc != fmtDest)
                throw new InvalidOperationException();

            nint ctx = 0;
            WarpCoreStatus status = WarpCoreStatus.WCORE_OK;
            unsafe
            {
                fixed (byte* ptrSrc = &MemoryMarshal.GetReference(rawSrc))
                fixed (byte* ptrDest = &MemoryMarshal.GetReference(rawDest))
                {
                    status = (WarpCoreStatus)WarpCore.tps_fit(3, source.VertexCount, (nint)ptrSrc, (nint)ptrDest, ref ctx);
                }
            }

            // TODO: check status

            return new Tps3dContext(ctx);
        }
    }
}
