using System;
using System.Runtime.InteropServices;
using Warp9.Data;

namespace Warp9.Native
{
    public class CpdConfiguration
    {
        public float Beta { get; set; } = 2.0f;
        public float Lambda { get; set; } = 2.0f;
        public float W { get; set; } = 0.1f;
        public int MaxIterations { get; set; } = 200;
        public float Tolerance { get; set; } = 5e-3f;
        public CpdInitMethod InitMethod { get; set; } = CpdInitMethod.CPD_INIT_CLUSTERED;
        public bool UseGpu { get; set; } = true;

        public CpdFlags Flags
        {
            get
            {
                CpdFlags ret = new CpdFlags();
                if (UseGpu) ret |= CpdFlags.CPD_USE_GPU;

                return ret;
            }
        }

        public CpdInfo ToCpdInfo(int m = 0, int n = 0)
        {
            return new CpdInfo()
            { 
                n = n,
                m = m,
                d = 3,
                lambda = Lambda,
                beta = Beta,
                w = W,
                maxit = MaxIterations,
                flags = (int)Flags,
                tol = Tolerance
            };
        }
    }

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

        public int NumVertices => cpdInfo.m;
        public int NumEigenvectors => cpdInfo.neigen;

        public WarpCoreStatus Register(PointCloud pclTarget, out PointCloud? pclBent, out CpdResult result)
        {
            CpdInfo info = cpdInfo;
            info.n = pclTarget.VertexCount;
            info.sigma2init = -1;

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
            CpdConfiguration cfg)
        {
            CpdInfo info = cfg.ToCpdInfo(pclFloating.VertexCount);

            if (!pclFloating.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> pclFloatingData))
                throw new InvalidOperationException();

            unsafe
            {
                int initDataSize = WarpCore.cpd_init(ref info, (int)cfg.InitMethod, nint.Zero, nint.Zero);
                byte[] initData = new byte[initDataSize];

                WarpCoreStatus ret = 0;
                fixed (byte* floatingDataPtr = &MemoryMarshal.GetReference(pclFloatingData))
                fixed (byte* initDataPtr = &MemoryMarshal.GetReference(initData.AsSpan()))
                {
                    ret = (WarpCoreStatus)WarpCore.cpd_init(ref info, (int)cfg.InitMethod, (nint)floatingDataPtr, (nint)initDataPtr);
                }

                if (ret == WarpCoreStatus.WCORE_OK)
                {
                    ctx = new CpdContext(info, initData, pclFloating, cfg.InitMethod);
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
