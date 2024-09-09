using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
            rangeStep = (range1 - range0) / width;
            bytesPerPixel = raw.Length / width;
        }

        byte[] data;
        SharpDX.DXGI.Format pixelFormat;
        int bytesPerPixel;
        float rangeMin, rangeMax, rangeStep;

        byte[] Data => data;

        public Lut Create(int width, float minValue, float maxValue, params (int, Color)[] stops)
        {
            byte[] raw = new byte[width * 4];
            // TODO: fill color by steps

            return new Lut(width, SharpDX.DXGI.Format.R8G8B8A8_UNorm, minValue, maxValue, raw);
        }
    }
}
