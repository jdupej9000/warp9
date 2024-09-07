using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    public static class RenderUtils
    {
        public static int GetStructSizeBytes(SharpDX.DXGI.Format fmt)
        {
            return fmt switch
            {
                SharpDX.DXGI.Format.R32_Float => 4,
                SharpDX.DXGI.Format.R32G32_Float => 8,
                SharpDX.DXGI.Format.R32G32B32_Float => 12,
                SharpDX.DXGI.Format.R32G32B32A32_Float => 16,
                SharpDX.DXGI.Format.R8_UInt => 1,
                SharpDX.DXGI.Format.R16_UInt => 2,
                SharpDX.DXGI.Format.R32_UInt => 4,
                SharpDX.DXGI.Format.B8G8R8A8_UNorm => 4,
                SharpDX.DXGI.Format.R8G8B8A8_UNorm => 4,
                _ => throw new NotSupportedException()
            };
        }

        public static SharpDX.Mathematics.Interop.RawColor4 ToDxColor(Color c)
        {
            return new SharpDX.Mathematics.Interop.RawColor4((float)c.R / 255.0f, (float)c.G / 255.0f, (float)c.B / 255.0f, (float)c.A / 255.0f);
        }

        public static System.Numerics.Vector4 ToNumColor(Color c)
        {
            return new System.Numerics.Vector4((float)c.R / 255.0f, (float)c.G / 255.0f, (float)c.B / 255.0f, (float)c.A / 255.0f);
        }

        public static byte[] ToByteArray<T>(ReadOnlySpan<T> data) where T : struct
        {
            int size = Marshal.SizeOf<T>() * data.Length;
            byte[] ret = new byte[size];
            MemoryMarshal.Cast<T, byte>(data).CopyTo(ret.AsSpan());
            return ret;
        }

    }
}
