﻿using System;
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
        public static Vector3 SampleTriangleBarycentric(ReadOnlySpan<byte> x, FaceIndices fi, int nv, float u, float v)
        {
            ReadOnlySpan<float> f = MemoryMarshal.Cast<byte, float>(x);

            Vector3 a = new Vector3(f[fi.I0], f[fi.I0 + nv], f[fi.I0 + 2 * nv]);
            Vector3 ba = new Vector3(f[fi.I1], f[fi.I1 + nv], f[fi.I1 + 2 * nv]) - a;
            Vector3 ca = new Vector3(f[fi.I2], f[fi.I2 + nv], f[fi.I2 + 2 * nv]) - a;

            return a + u * ba + v * ca;
        }
    }
}
