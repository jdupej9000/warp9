using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Warp9.Data
{
    public static class MeshUtils
    {
        public static readonly Dictionary<Type, SharpDX.DXGI.Format> TypeToDxgi = new Dictionary<Type, SharpDX.DXGI.Format>
        {
            { typeof(float), SharpDX.DXGI.Format.R32_Float },
            { typeof(Vector2), SharpDX.DXGI.Format.R32G32_Float },
            { typeof(Vector3), SharpDX.DXGI.Format.R32G32B32_Float },
            { typeof(Vector4), SharpDX.DXGI.Format.R32G32B32A32_Float },
            { typeof(FaceIndices), SharpDX.DXGI.Format.R32G32B32_UInt }
        };

       

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

        public static T[]? CopySoaToAos<T>(ReadOnlySpan<byte> src) where T:struct
        {
            int n = src.Length / Marshal.SizeOf<T>();
            T[] ret = new T[n];
            CopySoaToAos<T>(MemoryMarshal.Cast<T, byte>(ret.AsSpan()), src);
            return ret;
        }

        public static void CopySoaToAos<T>(Span<byte> dest, ReadOnlySpan<byte> src) where T:struct
        {
            if (typeof(T) == typeof(float))
            {
                CopySoaToAos(MemoryMarshal.Cast<byte, float>(dest), src);
            }
            else if (typeof(T) == typeof(Vector2))
            {
                CopySoaToAos(MemoryMarshal.Cast<byte, Vector2>(dest), src);
            }
            else if (typeof(T) == typeof(Vector3))
            {
                CopySoaToAos(MemoryMarshal.Cast<byte, Vector3>(dest), src);
            }
            else if (typeof(T) == typeof(Vector4))
            {
                CopySoaToAos(MemoryMarshal.Cast<byte, Vector3>(dest), src);
            }
            else
            {
                throw new NotSupportedException();
            }
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

        public static Mesh WithNormals(Mesh m)
        {
            MeshView? pos = m.GetView(MeshViewKind.Pos3f);
            if (pos is null || !pos.AsTypedData(out ReadOnlySpan<Vector3> posData))
                throw new InvalidOperationException();

            MeshView? faces = m.GetView(MeshViewKind.Indices3i);
            if (faces is null || !faces.AsTypedData(out ReadOnlySpan<FaceIndices> faceData))
                throw new InvalidOperationException();

            int nv = m.VertexCount;
            int nt = m.FaceCount;
            Vector3[] normal = new Vector3[nv];
            for (int i = 0; i < nv; i++)
                normal[i] = Vector3.Zero;

            for (int i = 0; i < nt; i++)
            {
                FaceIndices f = faceData[i];
                Vector3 a = posData[f.I0];
                Vector3 b = posData[f.I1];
                Vector3 c = posData[f.I2];
                Vector3 n = Vector3.Cross(b - a, c - a);

                normal[f.I0] += n;
                normal[f.I1] += n;
                normal[f.I2] += n;
            }

            MeshBuilder mb = m.ToBuilder();

            List<Vector3> normalsSeg = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Normal);
            for (int i = 0; i < nv; i++)
                normalsSeg.Add(Vector3.Normalize(normal[i]));

            return mb.ToMesh();
        }
    }
}
