using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Utils
{
    public static class MiscUtils
    {
        public static void Permute<T>(Span<T> dest, ReadOnlySpan<T> src, ReadOnlySpan<int> index)
           where T : struct
        {
            int n = index.Length;

            if (dest.Length < n || src.Length < n)
                throw new ArgumentException();

            for (int i = 0; i < n; i++)
                dest[i] = src[index[i]];
        }

        public static ReadOnlySpan<byte> ArrayToBytes(Array arr, int elemSize = -1)
        {
            ReadOnlySpan<byte> ret = MemoryMarshal.CreateReadOnlySpan(
                ref MemoryMarshal.GetArrayDataReference(arr),
                elemSize * arr.Length);

            return ret;
        }

        public static void Decompose(this Quaternion q, out Vector3 axis, out float angleDegrees)
        {
            Vector3 a = new Vector3(q.X, q.Y, q.Z);
            axis = Vector3.Normalize(a);

            float angle = 2.0f * MathF.Atan2(a.Length(), q.W);
            angleDegrees = 180.0f * angle / MathF.PI;
        }

        public static float FrobeniusNorm(this Matrix4x4 m)
        {
            return m.M11 * m.M11 + m.M12 * m.M12 + m.M13 * m.M13 + m.M14 * m.M14 +
                m.M21 * m.M21 + m.M22 * m.M22 + m.M23 * m.M23 + m.M24 * m.M24 +
                m.M31 * m.M31 + m.M32 * m.M32 + m.M33 * m.M33 + m.M34 * m.M34 +
                m.M41 * m.M41 + m.M32 * m.M42 + m.M43 * m.M43 + m.M44 * m.M44;
        }

        public static void Histogram(ReadOnlySpan<float> x, int bins, float x0, float x1, Span<int> hist, out int below, out int above)
        {
            float dx = bins / (x1 - x0);
            int numBelow = 0, numAbove = 0;

            for (int i = 0; i < x.Length; i++)
            {
                int bin = (int)((x[i] - x0) * dx);

                if (bin < 0) numBelow++;
                else if (bin >= bins) numAbove++;
                else hist[bin]++;
            }

            below = numBelow;
            above = numAbove;
        }

        public static int CumSum(Span<int> arr, int initial = 0)
        {
            int sum = initial;
            for (int i = 0; i < arr.Length; i++)
            {
                int sumNew = sum + arr[i];
                arr[i] = sum;
                sum = sumNew;
            }
            return sum;
        }

        public static void ThresholdBelow(ReadOnlySpan<float> x, float thresh, Span<bool> res)
        {
            if (x.Length != res.Length)
                throw new ArgumentException();

            for (int i = 0; i < x.Length; i++)
                res[i] = x[i] < thresh;
        }

        public static Vector2 Range(ReadOnlySpan<float> x)
        {
            if (x.Length == 0)
                return Vector2.Zero;

            float min = x[0], max = x[0];

            for (int i = 1; i < x.Length; i++)
            {
                if (x[i] < min) min = x[i];
                if (x[i] > max) max = x[i];
            }

            return new Vector2(min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FromSoa(ReadOnlySpan<byte> x, int idx, int nv)
        {
            ReadOnlySpan<float> f = MemoryMarshal.Cast<byte, float>(x);
            return new Vector3(f[idx], f[idx + nv], f[idx + 2 * nv]);       
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SampleTriangleBarycentric(ReadOnlySpan<Vector3> x, FaceIndices fi, int nv, float u, float v)
        {
          
            Vector3 a = x[fi.I0];
            Vector3 ba = x[fi.I1] - a;
            Vector3 ca = x[fi.I2] - a;

            return a + u * ba + v * ca;
        }

        public static MeshSegmentFormat TypeComposition<T>()
            where T : struct
        {
            if (typeof(T) == typeof(float))
                return MeshSegmentFormat.Float32;
            else if (typeof(T) == typeof(Vector2))
                return MeshSegmentFormat.Float32x2;
            else if (typeof(T) == typeof(Vector3))
                return MeshSegmentFormat.Float32x3;
            else if (typeof(T) == typeof(Vector4))
                return MeshSegmentFormat.Float32x4;
            else if (typeof(T) == typeof(Matrix4x4))
                return MeshSegmentFormat.Float32x16;
            else if (typeof(T) == typeof(uint))
                return MeshSegmentFormat.Int8x4;

                return MeshSegmentFormat.Unknown;
        }

        public static int GetNumStructElems(MeshSegmentFormat fmt)
        {
            return fmt switch
            {
                MeshSegmentFormat.Float32 => 1,
                MeshSegmentFormat.Float32x2 => 2,
                MeshSegmentFormat.Float32x3 => 3,
                MeshSegmentFormat.Float32x4 => 4,
                MeshSegmentFormat.Float32x16 => 16,
                MeshSegmentFormat.Int8x4 => 4,
                _ => 0
            };
        }

        public static int GetStructElemSize(MeshSegmentFormat fmt)
        {
            return fmt switch
            {
                MeshSegmentFormat.Float32 => 4,
                MeshSegmentFormat.Float32x2 => 4,
                MeshSegmentFormat.Float32x3 => 4,
                MeshSegmentFormat.Float32x4 => 4,
                MeshSegmentFormat.Float32x16 => 4,
                MeshSegmentFormat.Int8x4 => 1,
                _ => 0
            };
        }

        public static SharpDX.DXGI.Format GetDxgiFormat(MeshSegmentFormat fmt)
        {
            return fmt switch
            {
                MeshSegmentFormat.Float32 => SharpDX.DXGI.Format.R32_Float,
                MeshSegmentFormat.Float32x2 => SharpDX.DXGI.Format.R32G32_Float,
                MeshSegmentFormat.Float32x3 => SharpDX.DXGI.Format.R32G32B32_Float,
                MeshSegmentFormat.Float32x4 => SharpDX.DXGI.Format.R32G32B32A32_Float,
                MeshSegmentFormat.Int8x4 => SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                _ => SharpDX.DXGI.Format.Unknown
            };
        }
    }
}
