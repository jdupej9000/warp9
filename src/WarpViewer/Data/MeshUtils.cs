using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public static class MeshUtils
    {
        public static void CopySoaToAos(Span<float> dest, ReadOnlySpan<byte> src)
        {
            int n = src.Length / 4;
            if (dest.Length < n)
                throw new InvalidOperationException();

            ReadOnlySpan<float> x = MemoryMarshal.Cast<byte, float>(src);
            x.CopyTo(dest);
        }

        public static void CopySoaToAos(Span<Vector2> dest, ReadOnlySpan<byte> src)
        {
            int n = src.Length / 8;
            if (dest.Length < n)
                throw new InvalidOperationException();

            ReadOnlySpan<float> x = MemoryMarshal.Cast<byte, float>(src);
            ReadOnlySpan<float> y = x.Slice(n);

            for (int i = 0; i < n; i++)
                dest[i] = new Vector2(x[i], y[i]);
        }

        public static void CopySoaToAos(Span<Vector3> dest, ReadOnlySpan<byte> src)
        {
            int n = src.Length / 12;
            if (dest.Length < n) 
                throw new InvalidOperationException();

            ReadOnlySpan<float> x = MemoryMarshal.Cast<byte, float>(src);
            ReadOnlySpan<float> y = x.Slice(n);
            ReadOnlySpan<float> z = x.Slice(2 * n);

            for (int i = 0; i < n; i++)
                dest[i] = new Vector3(x[i], y[i], z[i]);
        }

        public static void CopySoaToAos(Span<Vector4> dest, ReadOnlySpan<byte> src)
        {
            int n = src.Length / 16;
            if (dest.Length < n)
                throw new InvalidOperationException();

            ReadOnlySpan<float> x = MemoryMarshal.Cast<byte, float>(src);
            ReadOnlySpan<float> y = x.Slice(n);
            ReadOnlySpan<float> z = x.Slice(2 * n);
            ReadOnlySpan<float> w = x.Slice(3 * n);

            for (int i = 0; i < n; i++)
                dest[i] = new Vector4(x[i], y[i], z[i], w[i]);
        }

        public static T[]? CopySoaToAos<T>(ReadOnlySpan<byte> src)
        {
            if (typeof(T) == typeof(float))
            {
                int n = src.Length / 4;
                float[] ret = new float[n];
                CopySoaToAos(ret.AsSpan(), src);
                return ret as T[];
            }
            else if (typeof(T) == typeof(Vector2))
            {
                int n = src.Length / 8;
                Vector2[] ret = new Vector2[n];
                CopySoaToAos(ret.AsSpan(), src);
                return ret as T[];
            }
            else if (typeof(T) == typeof(Vector3))
            {
                int n = src.Length / 12;
                Vector3[] ret = new Vector3[n];
                CopySoaToAos(ret.AsSpan(), src);
                return ret as T[];
            }
            else if (typeof(T) == typeof(Vector4))
            {
                int n = src.Length / 16;
                Vector4[] ret = new Vector4[n];
                CopySoaToAos(ret.AsSpan(), src);
                return ret as T[];
            }

            throw new NotSupportedException();
        }

        public static void CopyAosToSoaI4(Span<byte> dest, ReadOnlySpan<int> src, int srcStructSize)
        {
            Span<int> destF = MemoryMarshal.Cast<byte, int>(dest);
            int n = src.Length / srcStructSize;

            for (int i = 0; i < srcStructSize; i++)
            {
                Span<int> destFseg = destF.Slice(i * n);

                for (int j = 0; j < n; j++)
                    destFseg[j] = src[i + srcStructSize * j];
            }
        }

        public static void CopyAosToSoa<T>(Span<byte> dest, ReadOnlySpan<T> src) where T : struct
        {
            int structSize = Marshal.SizeOf<T>();

            if (structSize % 4 != 0)
                throw new NotSupportedException();

            CopyAosToSoaI4(dest, MemoryMarshal.Cast<T, int>(src), structSize / 4);
        }
    }
}
