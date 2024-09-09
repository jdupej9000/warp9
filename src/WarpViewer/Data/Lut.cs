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
        private Lut(int width, SharpDX.DXGI.Format fmt, float range0, float range1, byte[] raw)
        {
            data = raw;
            pixelFormat = fmt;
            rangeMin = range0;
            rangeMax = range1;
            rangeStep = width / (range1 - range0);
            bytesPerPixel = raw.Length / width;
            numPixels = width;
        }

        byte[] data;
        SharpDX.DXGI.Format pixelFormat;
        int bytesPerPixel, numPixels;
        float rangeMin, rangeMax, rangeStep;

        byte[] Data => data;

        public Color Sample(float x)
        {
            Span<Color> colors = MemoryMarshal.Cast<byte, Color>(data.AsSpan());
            int pos = Math.Clamp(0, numPixels - 1, (int)((x - rangeMin) * rangeStep));
            return colors[pos];
        }

        public static Lut Create(int width, float minValue, float maxValue, params (int, Color)[] stops)
        {
            byte[] raw = new byte[width * 4];
            Span<Color> colors = MemoryMarshal.Cast<byte, Color>(raw.AsSpan());

            int nstops = stops.Length - 1;
            for (int s = 0; s < nstops - 1; s++)
            {
                (int i0, Color c0) = stops[s];
                (int i1, Color c1) = stops[s + 1];

                Vector4 color0 = RenderUtils.ToNumColor(c0);
                Vector4 color1 = RenderUtils.ToNumColor(c1);

                for (int i = i0; i < i1; i++)
                {
                    Vector4 color = Vector4.Lerp(color0, color1, (float)(i - i0) / (i1 - i0));
                    colors[i] = RenderUtils.ToColor(color);
                }
            }

            (int ilast, Color clast) = stops[stops.Length - 1];
            for (int i = ilast; i < width; i++)
                colors[i] = clast;

            return new Lut(width, SharpDX.DXGI.Format.R8G8B8A8_UNorm, minValue, maxValue, raw);
        }

        public static readonly (int, Color)[] JetColors =
        {

        };
    }
}
