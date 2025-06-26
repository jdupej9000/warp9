using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
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
            Span<int> colors = MemoryMarshal.Cast<byte, int>(data.AsSpan());
            int pos = Math.Clamp((int)(x * numPixels), 0, numPixels - 1);
            return Color.FromArgb(colors[pos]);
        }

        public static Lut Create(int width, LutSpec spec)
        {
            byte[] raw = new byte[width * 4];
            Span<int> colors = MemoryMarshal.Cast<byte, int>(raw.AsSpan());

            spec.SampleRgba8(colors);

            return new Lut(width, SharpDX.DXGI.Format.R8G8B8A8_UNorm, raw);
        }

        public static Lut Create(int width, params (float, Color)[] stops)
        {
            return Create(width, new LutSpec(0, stops));
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

        public static readonly (float, Color)[] JetColors =
        {
            (0.00f, Color.FromArgb(0,0,127)),
            (0.10f, Color.FromArgb(0,0,229)),
            (0.20f, Color.FromArgb(0,76,255)),
            (0.30f, Color.FromArgb(0,178,255)),
            (0.40f, Color.FromArgb(25,255,229)),
            (0.50f, Color.FromArgb(127,255,127)),
            (0.60f, Color.FromArgb(229,255,25)),
            (0.70f, Color.FromArgb(255,178,0)),
            (0.80f, Color.FromArgb(255,76,0)),
            (0.90f, Color.FromArgb(229,0,0)),
            (1.00f, Color.FromArgb(127,0,0))
        };

        public static readonly (float, Color)[] BlueToGreenColors =
        {
            (0.00f, Color.FromArgb(7,63,128)),
            (0.50f, Color.FromArgb(103,191,203)),
            (1.00f, Color.FromArgb(223,242,218))
        };

        public static readonly (float, Color)[] GreyColors =
        {
            (0.00f, Color.FromArgb(0, 0, 0)),
            (1.00f, Color.FromArgb(255, 255, 255))
        };
    }
}
