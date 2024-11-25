using System;
using System.Runtime.InteropServices;
using Warp9.Data;

namespace Warp9.Native
{
    public class CpdContext
    {
        private CpdContext(CpdInfo info, byte[] initData, PointCloud pclFloat, CpdInitMethod im)
        {
            cpdInfo = info;
            cpdInitData = initData;
            pclFloating = pclFloat;
            initMethod = im;
        }

        CpdInfo cpdInfo;
        byte[] cpdInitData;
        PointCloud pclFloating;
        CpdInitMethod initMethod;

        public WarpCoreStatus Register(PointCloud pclTarget, out PointCloud? pclBent, out CpdResult result)
        {
            CpdInfo info = cpdInfo;
            info.n = pclTarget.VertexCount;

            CpdResult cpdRes = new CpdResult();
            int sizeBytesT = info.d * info.m * Marshal.SizeOf<float>();
            byte[] t = new byte[sizeBytesT];

            if (!pclTarget.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> x))
                throw new InvalidOperationException();

            if (!pclFloating.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> y))
                throw new InvalidOperationException();

            unsafe
            {
                fixed (byte* ptrT = &MemoryMarshal.GetReference(t.AsSpan()))
                fixed (byte* ptrInit = &MemoryMarshal.GetReference(cpdInitData.AsSpan()))
                fixed (byte* ptrX = &MemoryMarshal.GetReference(x))
                fixed (byte* ptrY = &MemoryMarshal.GetReference(y))
                {
                    WarpCoreStatus res = (WarpCoreStatus)WarpCore.cpd_process(ref info, (nint)ptrX, (nint)ptrY, (nint)ptrInit, (nint)ptrT, ref cpdRes);
                    pclBent = PointCloud.FromRawSoaPositions(info.m, t);
                   
                    result = cpdRes;
                    return res;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("Nonrigid LR-CPD: d={0}, m={1}, lambda={2}, beta={3}, w={4}, s2init={5}, maxit={6}, neig={7}, flags={8}, tol={9}, initSize={10} Bytes",
                cpdInfo.d, cpdInfo.m, cpdInfo.lambda, cpdInfo.beta, cpdInfo.w, cpdInfo.sigma2init, cpdInfo.maxit, cpdInfo.neigen, (CpdFlags)cpdInfo.flags, cpdInfo.tol, cpdInitData.Length);
        }

        public static WarpCoreStatus TryInitNonrigidCpd(out CpdContext? ctx, 
            PointCloud pclFloating,
            float lambda = 2,
            float beta = 2,
            float w = 0.1f,
            CpdInitMethod initMethod = CpdInitMethod.CPD_INIT_CLUSTERED,
            CpdFlags flags = CpdFlags.CPD_NONE,
            int maxIt = 200,
            float tol = 5e-3f)
        {
            CpdInfo info = new CpdInfo()
            {
                m = pclFloating.VertexCount,
                d = 3,
                n = 0,
                lambda = lambda,
                beta = beta,
                w = w,
                maxit = maxIt,
                flags = (int)flags,
                tol = tol
            };

            if (!pclFloating.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> pclFloatingData))
                throw new InvalidOperationException();

            unsafe
            {
                int initDataSize = WarpCore.cpd_init(ref info, (int)initMethod, nint.Zero, nint.Zero);
                byte[] initData = new byte[initDataSize];

                WarpCoreStatus ret = 0;
                fixed (byte* floatingDataPtr = &MemoryMarshal.GetReference(pclFloatingData))
                fixed (byte* initDataPtr = &MemoryMarshal.GetReference(initData.AsSpan()))
                {
                    ret = (WarpCoreStatus)WarpCore.cpd_init(ref info, (int)initMethod, (nint)floatingDataPtr, (nint)initDataPtr);
                }

                if (ret == WarpCoreStatus.WCORE_OK)
                {
                    ctx = new CpdContext(info, initData, pclFloating, initMethod);
                }
                else
                {
                    ctx = null;
                }

                return ret;
            }
        }
    }
}
