using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Warp9.Viewer;
using Warp9.ViewerOgl;

namespace Warp9.Test
{
    [TestClass]
    public class OpenGlViewerTests
    {
        const int Width = 256;
        const int Height = 256;

        private static void SaveResult(OffscreenRenderer r, string fileName)
        {
            string dir = Path.Combine(BitmapAsserts.ResultPath, "opengl");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, fileName);

            using Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format32bppRgb);
            BitmapData bmpData = bmp.LockBits(
                  new Rectangle(0, 0, bmp.Width, bmp.Height),
                  ImageLockMode.ReadWrite, bmp.PixelFormat);

            unsafe
            {                
                Span<byte> bmpSpan = new Span<byte>((void*)bmpData.Scan0, bmpData.Stride * bmpData.Height);               
                r.ExtractColor(bmpSpan);
            }

            bmp.UnlockBits(bmpData);
            bmp.Save(path);
        }

        private static OffscreenRenderer MakeRend()
        {
            OffscreenRenderer rend = OffscreenRenderer.Create();
            rend.Resize(Width, Height);

            Console.WriteLine("Vendor   : " + rend.DeviceVendor);
            Console.WriteLine("Renderer : " + rend.DeviceInfo);
            Console.WriteLine("Version  : " + rend.DeviceVersion);
            Console.WriteLine("GLSL     : " + rend.DeviceGlslVersion);

            return rend;
        }

        [TestMethod]
        public void BlankTest()
        {
            using OffscreenRenderer r = MakeRend();
            r.Render();
            SaveResult(r, "BlankTest.png");
        }
    }
}
