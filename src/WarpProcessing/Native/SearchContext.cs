using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Warp9.Data;

namespace Warp9.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ResultInfoTBary
    {
        public float t, u, v, w;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ResultInfoDPtBary
    {
        public float d, x, y, z, u, v, w, _reserved0;
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

        public bool NearestSoa(ReadOnlySpan<byte> srcSoa, int n, float maxDist, Span<int> hitIndex, Span<ResultInfoDPtBary> result)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            SearchQueryConfig cfg = new SearchQueryConfig();
            cfg.max_dist = maxDist;

            unsafe
            {
                fixed (byte* srcSoaPtr = &MemoryMarshal.GetReference(srcSoa))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (ResultInfoDPtBary* hitDistPtr = &MemoryMarshal.GetReference(result))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)SEARCH_KIND.SEARCH_NN_DPTBARY, ref cfg, 
                        (nint)srcSoaPtr, nint.Zero, n, (nint)hitIndexPtr, (nint)hitDistPtr);
                }
            }
        }

        public bool NearestAos(ReadOnlySpan<Vector3> srcSoa, int n, float maxDist, Span<int> hitIndex, Span<ResultInfoDPtBary> result)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            SearchQueryConfig cfg = new SearchQueryConfig();
            cfg.max_dist = maxDist;

            unsafe
            {
                fixed (Vector3* srcSoaPtr = &MemoryMarshal.GetReference(srcSoa))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (ResultInfoDPtBary* hitDistPtr = &MemoryMarshal.GetReference(result))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)SEARCH_KIND.SEARCH_NN_DPTBARY | (int)SEARCH_KIND.SEARCH_SOURCE_IS_AOS, ref cfg,
                        (nint)srcSoaPtr, nint.Zero, n, (nint)hitIndexPtr, (nint)hitDistPtr);
                }
            }
        }

        public bool RaycastSoa(ReadOnlySpan<byte> srcSoa, ReadOnlySpan<byte> srcDirSoa,  int n, Span<int> hitIndex, Span<float> hitT)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            SearchQueryConfig cfg = new SearchQueryConfig();

            unsafe
            {
                fixed (byte* srcSoaPtr = &MemoryMarshal.GetReference(srcSoa))
                fixed (byte* srcDirSoaPtr = &MemoryMarshal.GetReference(srcDirSoa))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (float* hitTPtr = &MemoryMarshal.GetReference(hitT))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)SEARCH_KIND.SEARCH_RAYCAST_T, ref cfg,
                        (nint)srcSoaPtr, (nint)srcDirSoaPtr, n, (nint)hitIndexPtr, (nint)hitTPtr);
                }
            }
        }

        public bool RaycastSoa(ReadOnlySpan<byte> srcSoa, ReadOnlySpan<byte> srcDirSoa, int n, Span<int> hitIndex, Span<ResultInfoTBary> hitT)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            SearchQueryConfig cfg = new SearchQueryConfig();

            unsafe
            {
                fixed (byte* srcSoaPtr = &MemoryMarshal.GetReference(srcSoa))
                fixed (byte* srcDirSoaPtr = &MemoryMarshal.GetReference(srcDirSoa))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (ResultInfoTBary* hitTPtr = &MemoryMarshal.GetReference(hitT))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)SEARCH_KIND.SEARCH_RAYCAST_TBARY, ref cfg,
                        (nint)srcSoaPtr, (nint)srcDirSoaPtr, n, (nint)hitIndexPtr, (nint)hitTPtr);
                }
            }
        }

        public bool RaycastAos(ReadOnlySpan<Vector3> src, ReadOnlySpan<Vector3> srcDir, int n, Span<int> hitIndex, Span<float> hitT)
        {
            if (structKind != SEARCH_STRUCTURE.SEARCH_TRIGRID3)
                return false;

            SearchQueryConfig cfg = new SearchQueryConfig();

            unsafe
            {
                fixed (Vector3* srcSoaPtr = &MemoryMarshal.GetReference(src))
                fixed (Vector3* srcDirSoaPtr = &MemoryMarshal.GetReference(srcDir))
                fixed (int* hitIndexPtr = &MemoryMarshal.GetReference(hitIndex))
                fixed (float* hitTPtr = &MemoryMarshal.GetReference(hitT))
                {
                    return WarpCoreStatus.WCORE_OK == (WarpCoreStatus)WarpCore.search_query(
                        nativeContext, (int)(SEARCH_KIND.SEARCH_RAYCAST_T | SEARCH_KIND.SEARCH_SOURCE_IS_AOS), ref cfg,
                        (nint)srcSoaPtr, (nint)srcDirSoaPtr, n, (nint)hitIndexPtr, (nint)hitTPtr);
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

        public static WarpCoreStatus TryInitTrigrid(Mesh m, int numCells, out SearchContext? searchCtx)
        {
            TriGridConfig cfg = new TriGridConfig() { num_cells = numCells };
            Span<TriGridConfig> cfgSpan = stackalloc TriGridConfig[1];
            cfgSpan[0] = cfg;

            if (!m.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> posRaw))
                throw new InvalidOperationException();

            bool isIndexed = m.TryGetIndexData(out ReadOnlySpan<byte> idxRaw);
            nint ctx = nint.Zero;

            int nt = m.FaceCount;
            int[] idxt = new int[nt * 3];
            ReadOnlySpan<int> idx = MemoryMarshal.Cast<byte, int>(idxRaw);
            for (int i = 0; i < nt; i++)
            {
                idxt[i] = idx[3 * i];
                idxt[i + nt] = idx[3 * i + 1];
                idxt[i + 2 * nt] = idx[3 * i + 2];
            }


            unsafe
            {
                fixed (byte* posRawPtr = &MemoryMarshal.GetReference(posRaw))
                //fixed (byte* idxRawPtr = &MemoryMarshal.GetReference(idxRaw))
                fixed (int* idxRawPtr = &MemoryMarshal.GetReference(idxt.AsSpan()))
                fixed (byte* cfgPtr = &MemoryMarshal.GetReference(MemoryMarshal.Cast<TriGridConfig, byte>(cfgSpan)))
                {
                    WarpCoreStatus s = (WarpCoreStatus)WarpCore.search_build((int)SEARCH_STRUCTURE.SEARCH_TRIGRID3, (nint)posRawPtr, (nint)idxRawPtr, m.VertexCount, m.FaceCount, (nint)cfgPtr, ref ctx);
                    if (s != WarpCoreStatus.WCORE_OK)
                    {
                        searchCtx = null;
                        return s;
                    }
                }
            }

            searchCtx = new SearchContext(ctx, SEARCH_STRUCTURE.SEARCH_TRIGRID3);
            return WarpCoreStatus.WCORE_OK;
        }
    }
}
