using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ResultInfoTBary
    {
        float t, u, v, w;
    }

    public class SearchContext : IDisposable
    {
        private SearchContext(nint nativeCtx, SEARCH_STRUCTURE sstruct)
        {
            nativeContext = nativeCtx;
            structKind = sstruct;
        }

        nint nativeContext;
        SEARCH_STRUCTURE structKind;

        public bool NearestSoa(ReadOnlySpan<byte> srcSoa, int n, Span<int> hitIndex, Span<float> hitDist)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            unsafe
            {
                fixed (byte* srcSoaPtr = &MemoryMarshal.GetReference(srcSoa))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (float* hitDistPtr = &MemoryMarshal.GetReference(hitDist))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)SEARCH_KIND.SEARCH_NN, (nint)srcSoaPtr, nint.Zero, n, (nint)hitIndexPtr, (nint)hitDistPtr);
                }
            }
        }

        public bool RaycastSoa(ReadOnlySpan<byte> srcSoa, ReadOnlySpan<byte> srcDirSoa,  int n, Span<int> hitIndex, Span<float> hitT)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            unsafe
            {
                fixed (byte* srcSoaPtr = &MemoryMarshal.GetReference(srcSoa))
                fixed (byte* srcDirSoaPtr = &MemoryMarshal.GetReference(srcDirSoa))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (float* hitTPtr = &MemoryMarshal.GetReference(hitT))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)SEARCH_KIND.SEARCH_RAYCAST_T, (nint)srcSoaPtr, (nint)srcDirSoaPtr, n, (nint)hitIndexPtr, (nint)hitTPtr);
                }
            }
        }

        public bool RaycastSoa(ReadOnlySpan<byte> srcSoa, ReadOnlySpan<byte> srcDirSoa, int n, Span<int> hitIndex, Span<ResultInfoTBary> hitT)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            unsafe
            {
                fixed (byte* srcSoaPtr = &MemoryMarshal.GetReference(srcSoa))
                fixed (byte* srcDirSoaPtr = &MemoryMarshal.GetReference(srcDirSoa))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (ResultInfoTBary* hitTPtr = &MemoryMarshal.GetReference(hitT))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)SEARCH_KIND.SEARCH_RAYCAST_TBARY, (nint)srcSoaPtr, (nint)srcDirSoaPtr, n, (nint)hitIndexPtr, (nint)hitTPtr);
                }
            }
        }

        public bool RaycastAos(ReadOnlySpan<Vector3> src, ReadOnlySpan<Vector3> srcDir, int n, Span<int> hitIndex, Span<float> hitT)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            unsafe
            {
                fixed (Vector3* srcSoaPtr = &MemoryMarshal.GetReference(src))
                fixed (Vector3* srcDirSoaPtr = &MemoryMarshal.GetReference(srcDir))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (float* hitTPtr = &MemoryMarshal.GetReference(hitT))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)(SEARCH_KIND.SEARCH_RAYCAST_T | SEARCH_KIND.SEARCH_SOURCE_IS_AOS), (nint)srcSoaPtr, (nint)srcDirSoaPtr, n, (nint)hitIndexPtr, (nint)hitTPtr);
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (nativeContext != nint.Zero)
            {
                WarpCore.search_free(nativeContext);
                nativeContext = nint.Zero;
            }
        }

        public static WarpCoreStatus TryInitSearch(Mesh m, SEARCH_STRUCTURE structKind, out SearchContext? searchCtx)
        {
            TriGridConfig cfg = new TriGridConfig() { num_cells = 16 };
            Span<TriGridConfig> cfgSpan = stackalloc TriGridConfig[1];
            cfgSpan[0] = cfg;

            if (!m.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> posRaw))
                throw new InvalidOperationException();

            bool isIndexed = m.TryGetIndexData(out ReadOnlySpan<byte> idxRaw);
            nint ctx = nint.Zero;

            unsafe
            {
                fixed (byte* posRawPtr = &MemoryMarshal.GetReference(posRaw))
                fixed (byte* idxRawPtr = &MemoryMarshal.GetReference(idxRaw))
                fixed (byte* cfgPtr = &MemoryMarshal.GetReference(MemoryMarshal.Cast<TriGridConfig, byte>(cfgSpan)))
                {
                    WarpCoreStatus s = (WarpCoreStatus)WarpCore.search_build((int)structKind, (nint)posRawPtr, (nint)idxRawPtr, m.VertexCount, m.FaceCount, (nint)cfgPtr, ref ctx);
                    if (s != WarpCoreStatus.WCORE_OK)
                    {
                        searchCtx = null;
                        return s;
                    }
                }
            }

            searchCtx = new SearchContext(ctx, structKind);
            return WarpCoreStatus.WCORE_OK;
        }
    }
}
