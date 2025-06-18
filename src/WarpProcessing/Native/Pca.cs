using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection.Metadata;
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
        public const int KeyScores = 3;

        public ReadOnlySpan<float> GetMean()
        {
            return pcsMean.AsSpan().Slice(0, Dimension);
        }

        public ReadOnlySpan<float> GetPrincipalComponent(int index)
        {
            return pcsMean.AsSpan().Slice((index + 1) * Dimension, Dimension);
        }

        public void Synthesize(Span<float> result, params (int, float)[] scores)
        {
            int d = Math.Min(result.Length, Dimension);

            for (int i = 0; i < d; i++)
                result[i] = pcsMean[i]; // initialize with mean

            foreach ((int, float) s in scores)
            {
                int offs = (s.Item1 + 1) * Dimension;
                float v = s.Item2;

                for (int i = 0; i < d; i++)
                    result[i] += v * pcsMean[offs + i];
            }
        }

        public bool TryGetScores(ReadOnlySpan<float> data, Span<float> scores)
        {
            if (data.Length < Dimension || scores.Length < NumPcs)
                return false;

            WarpCoreStatus ret = WarpCoreStatus.WCORE_INVALID_ARGUMENT;
            unsafe
            {
                fixed (int* pallow = &MemoryMarshal.GetReference(allow.AsSpan()))
                fixed (float* pmeanpcs = &MemoryMarshal.GetReference(pcsMean.AsSpan()))
                fixed (float* pdata = &MemoryMarshal.GetReference(data))
                fixed (float* pscores = &MemoryMarshal.GetReference(scores))
                {
                    ret = (WarpCoreStatus)WarpCore.pca_data_to_scores(ref info, (nint)pdata, (nint)pmeanpcs, (nint)pallow, (nint)pscores);
                }
            }

            return ret == WarpCoreStatus.WCORE_OK;
        }

        public bool TryGetScores(PointCloud pcl, Span<float> scores)
        {
            if (pcl.VertexCount * 3 != Dimension || scores.Length < NumPcs)
                return false;

            if (!pcl.TryGetRawDataSegment(MeshSegmentType.Position, -1, out int offset, out int length))
                return false;

            byte[] raw = pcl.RawData;

            WarpCoreStatus ret = WarpCoreStatus.WCORE_INVALID_ARGUMENT;
            unsafe
            {
                fixed (int* pallow = &MemoryMarshal.GetReference(allow.AsSpan()))
                fixed (float* pmeanpcs = &MemoryMarshal.GetReference(pcsMean.AsSpan()))
                fixed (byte* pdata = &MemoryMarshal.GetReference(raw.AsSpan()))
                fixed (float* pscores = &MemoryMarshal.GetReference(scores))
                {
                    ret = (WarpCoreStatus)WarpCore.pca_data_to_scores(ref info, (nint)pdata + offset, (nint)pmeanpcs, (nint)pallow, (nint)pscores);
                }
            }

            return ret == WarpCoreStatus.WCORE_OK;
        }

        public bool TryPredict(ReadOnlySpan<float> scores, Span<float> pred)
        {
            if (scores.Length < NumPcs || pred.Length < Dimension)
                return false;

            WarpCoreStatus ret = WarpCoreStatus.WCORE_INVALID_ARGUMENT;
            unsafe
            {
                fixed (float* pmeanpcs = &MemoryMarshal.GetReference(pcsMean.AsSpan()))
                fixed (float* ppred = &MemoryMarshal.GetReference(pred))
                fixed (float* pscores = &MemoryMarshal.GetReference(scores))
                {
                    ret = (WarpCoreStatus)WarpCore.pca_scores_to_data(ref info, (nint)pscores, (nint)pmeanpcs, (nint)ppred);
                }
            }

            return ret == WarpCoreStatus.WCORE_OK;
        }

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
            if (mc.TryGetMatrix(KeyPcsMean, out Matrix<float>? matPcsMean) &&
                matPcsMean is not null &&
                mc.TryGetMatrix(KeyPcVariance, out Matrix<float>? matVar) &&
                matVar is not null)
            {
                PcaInfo info = new PcaInfo()
                {
                    m = matPcsMean.Rows,
                    n = matPcsMean.Columns - 1,
                    npcs = mc.Count
                };

                int[]? allow = null;
                if (mc.TryGetMatrix(KeyAllow, out Matrix<int>? matAllow) && matAllow is not null)
                    allow = matAllow.Data;

                if (allow is null)
                {
                    int numAllow = (matPcsMean.Rows + 31) / 32;
                    int[] allowBitField = new int[numAllow];
                    for (int i = 0; i < numAllow; i++)
                        allowBitField[i] = -1;
                    allow = allowBitField;
                }

                return new Pca(PcaSourceDataKind.VertexPositions, matPcsMean.Data, matVar.Data, allow, info);
            }

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
            if (scale) pcaInfo.flags |= (int)PCA_FLAGS.PCA_SCALE_TO_UNITY;

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
