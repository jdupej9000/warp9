using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Warp9.Test
{
    public static class BitmapAsserts
    {
        public static readonly string ResultPath = @"../../bin/testresults";
        public static void AssertEqual(string reference, Bitmap testBitmap)
        {
            string refPath = Path.GetFullPath(Path.Combine(TestUtils.AssetsPath, reference));

            Directory.CreateDirectory(Path.GetFullPath(ResultPath));
            testBitmap.Save(Path.GetFullPath(Path.Combine(ResultPath, reference)));

            if (!File.Exists(refPath))
                Assert.Inconclusive("Could not find reference file: " + refPath);

            using Bitmap refBitmap = new Bitmap(refPath);
            Assert.AreEqual(refBitmap.Width, testBitmap.Width);
            Assert.AreEqual(refBitmap.Height, testBitmap.Height);
            Assert.AreEqual(refBitmap.PixelFormat, testBitmap.PixelFormat);

            BitmapData dataTest = testBitmap.LockBits(
                new Rectangle(0, 0, testBitmap.Width, testBitmap.Height),
                ImageLockMode.ReadWrite, testBitmap.PixelFormat);

            BitmapData dataRef = refBitmap.LockBits(
               new Rectangle(0, 0, refBitmap.Width, refBitmap.Height),
               ImageLockMode.ReadWrite, refBitmap.PixelFormat);

            unsafe
            {
                byte* ptrTest = (byte*)dataTest.Scan0;
                byte* ptrRef = (byte*)dataRef.Scan0;

                switch (testBitmap.PixelFormat)
                {
                    case PixelFormat.Format8bppIndexed:
                        AssertEqualGray8(ptrRef, ptrTest, refBitmap.Width, dataRef.Stride, refBitmap.Height, 0);
                        break;

                    case PixelFormat.Format32bppRgb:
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppPArgb:
                        AssertEqualRgba8(ptrRef, ptrTest, refBitmap.Width, dataRef.Stride, refBitmap.Height, 0);
                        break;

                    default:
                        Assert.Inconclusive("Pixel format " + testBitmap.PixelFormat + " is not supported.");
                        break;
                }

            }

            refBitmap.UnlockBits(dataRef);
            testBitmap.UnlockBits(dataTest);
        }

        private static unsafe void AssertEqualGray8(byte* ptrRef, byte* ptrTest, int width, int stride, int height, int tol = 16)
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

        private static unsafe void AssertEqualRgba8(byte* ptrRef, byte* ptrTest, int width, int stride, int height, int tol = 16)
        {
            int maxError = 0;
            int numTolExceeded = 0;

            for (int y = 0; y < height; y++)
            {
                ReadOnlySpan<int> sRef = new ReadOnlySpan<int>(ptrRef + y * stride, width);
                ReadOnlySpan<int> sTest = new ReadOnlySpan<int>(ptrTest + y * stride, width);

                for (int x = 0; x < width; x++)
                {
                    int e0 = (sRef[x] & 0xff) - (sTest[x] & 0xff);
                    int e1 = ((sRef[x] >> 8) & 0xff) - ((sTest[x] >> 8) & 0xff);
                    int e2 = ((sRef[x] >> 16) & 0xff) - ((sTest[x] >> 16) & 0xff);
                    int e = Math.Max(e0, Math.Max(e1, e2));

                    if (e > tol) numTolExceeded++;
                    if (e > maxError) maxError = e;
                }
            }

            Assert.AreEqual(0, numTolExceeded);
        }
    }
}
