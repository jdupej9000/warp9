using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Warp9.Data;

namespace Warp9.Test
{
    public static class BitmapAsserts
    {
        public static readonly string ResultPath = @"../../bin/testresults";
        public static bool AssertEqual(string reference, RasterImage testBitmap, bool softInconclusive = false)
        {
            bool inconclusive = false;
            string refPath = Path.GetFullPath(Path.Combine(TestUtils.AssetsPath, reference));

            Directory.CreateDirectory(Path.GetFullPath(ResultPath));
            testBitmap.Save(Path.GetFullPath(Path.Combine(ResultPath, reference)));

            if (File.Exists(refPath))
            {
                RasterImage refBitmap = RasterImage.FromFile(refPath);
                switch(testBitmap.PixelFormat)
                {
                    case PixelFormat.Gray8:
                        AssertEqualGray8(refBitmap.GetRawData(), testBitmap.GetRawData(), refBitmap.Width, refBitmap.Stride, refBitmap.Height, 0);
                        break;

                    case PixelFormat.Rgbx8:
                    case PixelFormat.Rgba8:
                    case PixelFormat.Bgra8:
                    case PixelFormat.Bgrx8:
                        AssertEqualRgba8(refBitmap.GetRawData(), testBitmap.GetRawData(), refBitmap.Width, refBitmap.Stride, refBitmap.Height, MustFlipBgra(testBitmap, refBitmap), 0);
                        break;

                     default:
                            Console.WriteLine("Pixel format " + testBitmap.PixelFormat + " is not supported.");
                            inconclusive = true;
                            break;                       

                }
            }
            else
            {
                Console.WriteLine("Could not find reference file: " + refPath);
                inconclusive = true;
            }

            if (!softInconclusive && inconclusive)
                Assert.Inconclusive("Test inconclusive, see console output.");

            return inconclusive;
        }

        private static void AssertEqualGray8(ReadOnlySpan<byte> ptrRef, ReadOnlySpan<byte> ptrTest, int width, int stride, int height, int tol = 16)
        {
            int maxError = 0;
            int numTolExceeded = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int e = Math.Abs((int)ptrRef[y * stride + x] - (int)ptrTest[y * stride + x]);

                    if(e > tol) numTolExceeded++;
                    if(e > maxError) maxError = e;
                }
            }

            Assert.AreEqual(0, numTolExceeded);
        }

        private static bool MustFlipBgra(RasterImage ri0, RasterImage ri1)
        {
            PixelFormatInfo pfi0 = RasterImage.GetPixelFormatInfo(ri0.PixelFormat);
            PixelFormatInfo pfi1 = RasterImage.GetPixelFormatInfo(ri1.PixelFormat);
            return pfi0.RedOffsetBit == pfi1.BlueOffsetBit;
        }

        private static void AssertEqualRgba8(ReadOnlySpan<byte> ptrRef, ReadOnlySpan<byte> ptrTest, int width, int stride, int height, bool flipBgra, int tol = 16)
        {
            int maxError = 0;
            int numTolExceeded = 0;

            for (int y = 0; y < height; y++)
            {
                ReadOnlySpan<int> sRef = MemoryMarshal.Cast<byte, int>(ptrRef);
                ReadOnlySpan<int> sTest = MemoryMarshal.Cast<byte, int>(ptrTest);

                for (int x = 0; x < width; x++)
                {
                    int e0, e1, e2;
                    if(flipBgra)
                    {
                        e0 = ((sRef[x] >> 16) & 0xff) - (sTest[x] & 0xff);
                        e1 = ((sRef[x] >> 8) & 0xff) - ((sTest[x] >> 8) & 0xff);
                        e2 = (sRef[x] & 0xff) - ((sTest[x] >> 16) & 0xff);
                    }
                    else
                    {
                        e0 = (sRef[x] & 0xff) - (sTest[x] & 0xff);
                        e1 = ((sRef[x] >> 8) & 0xff) - ((sTest[x] >> 8) & 0xff);
                        e2 = ((sRef[x] >> 16) & 0xff) - ((sTest[x] >> 16) & 0xff);                        
                    }
                    int e = Math.Max(e0, Math.Max(e1, e2));
                    if (e > tol) numTolExceeded++;
                    if (e > maxError) maxError = e;
                }
            }

            Assert.AreEqual(0, numTolExceeded);
        }
    }
}
