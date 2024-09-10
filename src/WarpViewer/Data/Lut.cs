using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Warp9.Viewer;

namespace Warp9.Data
{
    public class Lut
    {
        private Lut(int width, SharpDX.DXGI.Format fmt, byte[] raw)
        {
            data = raw;
            pixelFormat = fmt;
            bytesPerPixel = raw.Length / width;
            numPixels = width;
        }

        readonly byte[] data;
        readonly SharpDX.DXGI.Format pixelFormat;
        readonly int bytesPerPixel, numPixels;

        public byte[] Data => data;
        public int NumPixels => numPixels;

        public Color Sample(float x)
        {
            Span<Color> colors = MemoryMarshal.Cast<byte, Color>(data.AsSpan());
            int pos = Math.Clamp(0, numPixels - 1, (int)(x * numPixels));
            return colors[pos];
        }

        public static Lut Create(int width, params (float, Color)[] stops)
        {
            byte[] raw = new byte[width * 4];
            Span<Color> colors = MemoryMarshal.Cast<byte, Color>(raw.AsSpan());

            int nstops = stops.Length - 1;
            for (int s = 0; s < nstops - 1; s++)
            {
                (float f0, Color c0) = stops[s];
                (float f1, Color c1) = stops[s + 1];
                int i0 = (int)(width * f0);
                int i1 = (int)(width * f1);

                Vector4 color0 = RenderUtils.ToNumColor(c0);
                Vector4 color1 = RenderUtils.ToNumColor(c1);

                for (int i = i0; i < Math.Min(width - 1, i1); i++)
                {
                    Vector4 color = Vector4.Lerp(color0, color1, (float)(i - i0) / (i1 - i0));
                    colors[i] = RenderUtils.ToColor(color);
                }
            }

            (float flast, Color clast) = stops[stops.Length - 1];
            int ilast = (int)(width * flast);
            for (int i = ilast; i < width; i++)
                colors[i] = clast;

            return new Lut(width, SharpDX.DXGI.Format.R8G8B8A8_UNorm, raw);
        }

        public static Lut CreateQuantized(int width, int numStops, params (float, Color)[] stops)
        {
            throw new NotImplementedException();
        }

        // https://www.kennethmoreland.com/color-advice/
        public static readonly (float, Color)[] FastColors =
        {
            (0.00f, Color.FromArgb(14, 14, 20)),
            (0.17f, Color.FromArgb(62, 117, 207)),
            (0.30f, Color.FromArgb(91, 190, 243)),
            (0.43f, Color.FromArgb(175, 237, 234)),
            (0.50f, Color.FromArgb(229, 241, 196)),
            (0.59f, Color.FromArgb(224, 213, 130)),
            (0.71f, Color.FromArgb(137, 158, 80)),
            (0.85f, Color.FromArgb(204, 90, 41)),
            (1.00f, Color.FromArgb(150, 20, 30))
        };

        public static readonly (float, Color)[] SmoothCoolWarmColors =
        {
            (0.00f, Color.FromArgb(59, 76, 192)),
            (0.50f, Color.FromArgb(221, 221, 221)),
            (1.00f, Color.FromArgb(180, 4, 38))
        };

        public static readonly (float, Color)[] BentCoolWarmColors =
        {
            (0.00f, Color.FromArgb(59, 76, 192)),
            (0.50f, Color.FromArgb(242, 242, 242)),
            (1.00f, Color.FromArgb(180, 4, 38))
        };

        public static readonly (float, Color)[] ViridisColors =
        {
            (0.00f, Color.FromArgb(68, 1, 84)),
            (0.14f, Color.FromArgb(70, 50, 127)),
            (0.29f, Color.FromArgb(54, 92, 141)),
            (0.43f, Color.FromArgb(39, 127, 142)),
            (0.57f, Color.FromArgb(31, 161, 135)),
            (0.71f, Color.FromArgb(74, 194, 109)),
            (0.86f, Color.FromArgb(159, 218, 58)),
            (1.00f, Color.FromArgb(253, 231, 37))
        };

        public static readonly (float, Color)[] PlasmaColors =
        {
            (0.00f, Color.FromArgb(13, 8, 135)),
            (0.14f, Color.FromArgb(84, 2, 163)),
            (0.29f, Color.FromArgb(139, 10, 165)),
            (0.43f, Color.FromArgb(185, 50, 137)),
            (0.57f, Color.FromArgb(219, 92, 104)),
            (0.71f, Color.FromArgb(244, 136, 73)),
            (0.86f, Color.FromArgb(254, 188, 43)),
            (1.00f, Color.FromArgb(240, 249, 33))
        };

        public static readonly (float, Color)[] BlackBodyColors =
       {
            (0.00f, Color.FromArgb(0, 0, 0)),
            (0.39f, Color.FromArgb(178, 34, 34)),
            (0.58f, Color.FromArgb(227, 105, 5)),
            (0.89f, Color.FromArgb(230, 230, 53)),
            (1.00f, Color.FromArgb(255, 255, 255))
        };
    }
}
