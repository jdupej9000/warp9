using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Native
{
    public enum PcaSourceDataKind
    {
        VertexPositions,
        General
    }

    public class Pca
    {
        private Pca(PcaSourceDataKind kind, float[] pcsMean, float[] variance, int[] allowBitfield, PcaInfo info)
        {
            SourceKind = kind;
            this.pcsMean = pcsMean;
            PcVariance = variance;
            allow = allowBitfield;
            this.info = info;
        }

        PcaInfo info;
        float[] pcsMean;
        int[]? allow;

        public PcaSourceDataKind SourceKind { get; }
        public float[] PcVariance { get; }
        public int NumPcs => info.npcs;
        public int Dimension => info.m;
        public int NumSourceData => info.n;

        public const int KeyPcsMean = 0;
        public const int KeyPcVariance = 1;
        public const int KeyAllow = 2;

        public MatrixCollection ToMatrixCollection()
        {
            MatrixCollection ret = new MatrixCollection();
            ret[KeyPcsMean] = new Matrix<float>(pcsMean, info.n + 1, info.m);
            ret[KeyPcVariance] = new Matrix<float>(PcVariance);

            if (allow is not null)
                ret[KeyAllow] = new Matrix<int>(allow);

            return ret;
        }

        public static int[] MakeBitField(bool[] data, int repeat = 1)
        {
            int len = (data.Length * repeat + 31) / 32;
            int[] ret = new int[len];

            int accum = 0, pos = 0, retpos = 0;
            for (int rep = 0; rep < repeat; rep++)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i])
                        accum |= 1 << pos;

                    pos++;

                    if (pos == 32)
                    {
                        ret[retpos++] = accum;
                        accum = 0;
                        pos = 0;
                    }
                }
            }

            if (pos != 0)
                ret[retpos] = accum;

            return ret;
        }

        public static Pca? FromMatrixCollection(MatrixCollection mc)
        {
            // TODO
            return null;
        }

        public static Pca? Fit(IReadOnlyList<PointCloud> pcls, bool[] vertexAllow, bool scale = false)
        {
            int n = pcls.Count;
            int m = 3 * pcls[0].VertexCount;

            GCHandle[] pins = new GCHandle[n];
            nint[] handles = new nint[n];
            for (int i = 0; i < n; i++)
            {
                pcls[i].TryGetRawDataSegment(MeshSegmentType.Position, -1, out int offset, out int length);
                // TODO: check length

                pins[i] = GCHandle.Alloc(pcls[i].RawData, GCHandleType.Pinned);
                handles[i] = pins[i].AddrOfPinnedObject() + offset;
            }

            int[] allowBitField = MakeBitField(vertexAllow, 3);

            PcaInfo pcaInfo = new PcaInfo { m = m, n = n, npcs = n, flags = 0 };

            float[] pcsMean = new float[(n + 1) * m];
            float[] pcVar = new float[n];

            WarpCoreStatus ret = WarpCoreStatus.WCORE_INVALID_ARGUMENT;
            unsafe
            {
                fixed (nint* ppdata = &MemoryMarshal.GetReference(handles.AsSpan()))
                fixed (int* pallow = &MemoryMarshal.GetReference(allowBitField.AsSpan()))
                fixed (float* pmeanpcs = &MemoryMarshal.GetReference(pcsMean.AsSpan()))
                fixed (float* pvar = &MemoryMarshal.GetReference(pcVar.AsSpan()))
                {
                    ret = (WarpCoreStatus)WarpCore.pca_fit(ref pcaInfo, (nint)ppdata, (nint)pallow, (nint)pmeanpcs, (nint)pvar);
                }
            }

            if (ret != WarpCoreStatus.WCORE_OK)
                return null;

            return new Pca(PcaSourceDataKind.VertexPositions, pcsMean, pcVar, allowBitField, pcaInfo);
        }

        public static Pca? Fit(float[] mat, int cols, bool scale = false)
        {
            int n = mat.Length / cols;
            int m = cols;

            GCHandle matPin = GCHandle.Alloc(mat, GCHandleType.Pinned);
            nint matPtr = matPin.AddrOfPinnedObject();

            nint[] handles = new nint[n];
            for (int i = 0; i < n; i++)
                handles[i] = matPtr + 4 * i * m;

            // Make a full whitelist until the native side can support that implicitly.
            int numAllow = (m + 31) / 32;
            int[] allowBitField = new int[numAllow];
            for (int i = 0; i < numAllow; i++)
                allowBitField[i] = -1;

            PcaInfo pcaInfo = new PcaInfo { m = m, n = n, npcs = n, flags = 0 };
            float[] pcsMean = new float[(n + 1) * m];
            float[] pcVar = new float[n];

            WarpCoreStatus ret = WarpCoreStatus.WCORE_INVALID_ARGUMENT;
            unsafe
            {
                fixed (nint* ppdata = &MemoryMarshal.GetReference(handles.AsSpan()))
                fixed (int* pallow = &MemoryMarshal.GetReference(allowBitField.AsSpan()))
                fixed (float* pmeanpcs = &MemoryMarshal.GetReference(pcsMean.AsSpan()))
                fixed (float* pvar = &MemoryMarshal.GetReference(pcVar.AsSpan()))
                {
                    ret = (WarpCoreStatus)WarpCore.pca_fit(ref pcaInfo, (nint)ppdata, (nint)pallow, (nint)pmeanpcs, (nint)pvar);
                }
            }

            if (ret != WarpCoreStatus.WCORE_OK)
                return null;

            return new Pca(PcaSourceDataKind.General, pcsMean, pcVar, allowBitField, pcaInfo);
        }
    }
}
