using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Warp9.Viewer;

namespace Warp9.Data
{
    public class Lut
    {
        private Lut(int width, PixelFormat fmt, byte[] raw)
        {
            data = raw;
            pixelFormat = fmt;
            bytesPerPixel = raw.Length / width;
            numPixels = width;
        }

        readonly byte[] data;
        readonly PixelFormat pixelFormat;
        readonly int bytesPerPixel, numPixels;

        public byte[] Data => data;
        public int NumPixels => numPixels;

        public Color Sample(float x)
        {
            Span<int> colors = MemoryMarshal.Cast<byte, int>(data.AsSpan());
            int pos = Math.Clamp((int)(x * numPixels), 0, numPixels - 1);
            return new Color(unchecked((uint)colors[pos]));
        }

        public static Lut Create(int width, LutSpec spec)
        {
            byte[] raw = new byte[width * 4];
            Span<int> colors = MemoryMarshal.Cast<byte, int>(raw.AsSpan());

            spec.SampleRgba8(colors);

            return new Lut(width, PixelFormat.Rgba8, raw);
        }

        public static Lut Create(int width, params (float, Color)[] stops)
        {
            return Create(width, new LutSpec(0, stops));
        }

        public static Lut Create(int width, int steps, params (float, Color)[] stops)
        {
            return Create(width, new LutSpec(steps, stops));
        }

        // https://www.kennethmoreland.com/color-advice/
        public static readonly (float, Color)[] FastColors =
        {
            (0.00f, new Color(14, 14, 20)),
            (0.17f, new Color(62, 117, 207)),
            (0.30f, new Color(91, 190, 243)),
            (0.43f, new Color(175, 237, 234)),
            (0.50f, new Color(229, 241, 196)),
            (0.59f, new Color(224, 213, 130)),
            (0.71f, new Color(137, 158, 80)),
            (0.85f, new Color(204, 90, 41)),
            (1.00f, new Color(150, 20, 30))
        };


        public static readonly (float, Color)[] ViridisColors =
        {
            (0.00f, new Color(68, 1, 84)),
            (0.14f, new Color(70, 50, 127)),
            (0.29f, new Color(54, 92, 141)),
            (0.43f, new Color(39, 127, 142)),
            (0.57f, new Color(31, 161, 135)),
            (0.71f, new Color(74, 194, 109)),
            (0.86f, new Color(159, 218, 58)),
            (1.00f, new Color(253, 231, 37))
        };

        public static readonly (float, Color)[] PlasmaColors =
        {
            (0.00f, new Color(13, 8, 135)),
            (0.14f, new Color(84, 2, 163)),
            (0.29f, new Color(139, 10, 165)),
            (0.43f, new Color(185, 50, 137)),
            (0.57f, new Color(219, 92, 104)),
            (0.71f, new Color(244, 136, 73)),
            (0.86f, new Color(254, 188, 43)),
            (1.00f, new Color(240, 249, 33))
        };

       public static readonly (float, Color)[] BlackBodyColors =
       {
            (0.00f, new Color(0, 0, 0)),
            (0.39f, new Color(178, 34, 34)),
            (0.58f, new Color(227, 105, 5)),
            (0.89f, new Color(230, 230, 53)),
            (1.00f, new Color(255, 255, 255))
        };

        public static readonly (float, Color)[] JetColors =
        {
            (0.00f, new Color(0,0,127)),
            (0.10f, new Color(0,0,229)),
            (0.20f, new Color(0,76,255)),
            (0.30f, new Color(0,178,255)),
            (0.40f, new Color(25,255,229)),
            (0.50f, new Color(127,255,127)),
            (0.60f, new Color(229,255,25)),
            (0.70f, new Color(255,178,0)),
            (0.80f, new Color(255,76,0)),
            (0.90f, new Color(229,0,0)),
            (1.00f, new Color(127,0,0))
        };

        public static readonly (float, Color)[] BlueToGreenColors =
        {
            (0.00f, new Color(7,63,128)),
            (0.50f, new Color(103,191,203)),
            (1.00f, new Color(223,242,218))
        };

        public static readonly (float, Color)[] GreyColors =
        {
            (0.00f, new Color(0, 0, 0)),
            (1.00f, new Color(255, 255, 255))
        };
    }
}
