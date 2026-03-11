using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Warp9.Data;

namespace Warp9.Test
{
    [TestClass]
    public class LutTests
    {
        private static void LutSampleAtStopCase(int width, (float, Color)[] stops)
        {
            Lut lut = Lut.Create(width, stops);
            foreach(var s in stops)
                Assert.AreEqual(s.Item2, lut.Sample(s.Item1));
        }

        private static Bitmap DebugLut(string filename, Lut lut)
        {
            int width = lut.NumPixels;
            int height = 8;
            Bitmap bmp = new Bitmap(width, height);

            unsafe
            {
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);

                for (int j = 0; j < height; j++)
                {
                    nint ptr = bmpData.Scan0 + j * bmpData.Stride;
                    Span<int> ptrSpan = new Span<int>((void*)ptr, bmpData.Stride);

                    for (int i = 0; i < width; i++)
                    {
                        float r = (float)i / width;                       
                        ptrSpan[i] = lut.Sample(r).ToArgb();                        
                    }
                }

                bmp.UnlockBits(bmpData);
            }

            TestUtils.SaveTestResult(filename, bmp);
            return bmp;
        }

        private static int CountColors(Bitmap bmp)
        {
            int width = bmp.Width; 
            int height = bmp.Height;
            HashSet<int> colors = new HashSet<int>();
            unsafe
            {
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);

                for (int j = 0; j < height; j++)
                {
                    nint ptr = bmpData.Scan0 + j * bmpData.Stride;
                    Span<int> ptrSpan = new Span<int>((void*)ptr, bmpData.Stride);

                    for (int i = 0; i < width; i++)
                    {
                        colors.Add(ptrSpan[i]);
                    }
                }

                bmp.UnlockBits(bmpData);
            }

            return colors.Count;
        }


        [TestMethod]
        public void LutCreateTest()
        {
            Lut lut = Lut.Create(256, Lut.FastColors);
            Assert.AreEqual(256, lut.NumPixels);
        }

        [TestMethod]
        public void LutSampleAtStopTest()
        {
            LutSampleAtStopCase(128, Lut.FastColors);
            LutSampleAtStopCase(256, Lut.FastColors);
            LutSampleAtStopCase(1024, Lut.FastColors);
            LutSampleAtStopCase(512, Lut.ViridisColors);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(4)]
        [DataRow(10)]
        [DataRow(32)]
        public void LutSegmentsTest(int seg)
        {
            const int width = 256;         
            using Bitmap bmp = DebugLut($"LutSegmentTest-{seg}.png", Lut.Create(width, seg, Lut.ViridisColors));

            int numColorsActual = CountColors(bmp);
            Assert.AreEqual(seg, numColorsActual);
        }

        [TestMethod]
        public void LutSmoothTest()
        {
            const int width = 256;
            using Bitmap bmp = DebugLut($"LutSmoothTest.png", Lut.Create(width, Lut.ViridisColors));
        }
    }
}