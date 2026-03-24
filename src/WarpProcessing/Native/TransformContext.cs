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
    public class TransformContext : IDisposable
    {
        private TransformContext(nint ctx)
        {
            nativeCtx = ctx;
        }

        nint nativeCtx;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (nativeCtx != nint.Zero)
            {
                WarpCore.transform_destroy(nativeCtx);
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
                    WarpCore.transform_apply(nativeCtx, x.Length, (nint)ptrX, (nint)ptrY);
                }
            }
        }

        public PointCloud TransformPosition(PointCloud pointCloud)
        {
            int nv = pointCloud.VertexCount;
            byte[] dest = new byte[nv * 12];
            pointCloud.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> srcRaw, out _);
            Transform(MemoryMarshal.Cast<byte, Vector3>(srcRaw), MemoryMarshal.Cast<byte, Vector3>(dest.AsSpan()));
            return PointCloud.FromRawPositions(nv, dest);
        }

        public static TransformContext FitTps(PointCloud source, PointCloud target)
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
                    FitTransformInfo info = new FitTransformInfo() {
                        kind = TRANSFORM_KIND.TPS,
                        dimension = 3,
                        flags = 0
                    };

                    status = (WarpCoreStatus)WarpCore.transform_fit(ref info, source.VertexCount, (nint)ptrSrc, (nint)ptrDest, ref ctx);
                }
            }

            return new TransformContext(ctx);
        }

        public static TransformContext FitLsTps(PointCloud source, PointCloud target, int[] knotIdx)
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
                fixed (int* ptrKnotIdx = knotIdx)
                {
                    FitTransformInfo info = new FitTransformInfo()
                    {
                        kind = TRANSFORM_KIND.LSTPS,
                        dimension = 3,
                        flags = 0,
                        num_ctl_points = knotIdx.Length,
                        ctl_idx = (nint)ptrKnotIdx
                    };

                    status = (WarpCoreStatus)WarpCore.transform_fit(ref info, source.VertexCount, (nint)ptrSrc, (nint)ptrDest, ref ctx);
                }
            }

            return new TransformContext(ctx);
        }
    }
}
