using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Utils;
using Warp9.Viewer;
using static System.Net.Mime.MediaTypeNames;

namespace Warp9.Test
{
    [TestClass]
    public class TextRenderingTests
    {
        private static FontDefinition LoadMinimalFont()
        {
            using Stream stream = TestUtils.OpenAsset("segoe-ui-minimal.fnt");
            return FontDefinition.FromStream(stream, TestUtils.AssetsPath);
        }

        private static (HeadlessRenderer, RenderItemHud) CreateRendererHud()
        {
            int adapter = TestUtils.FindAdapter();

            Assert.IsTrue(HeadlessRenderer.TryCreate(adapter, out HeadlessRenderer? r));
            Assert.IsNotNull(r);

            r.CanvasColor = Color.Black;

            r.Shaders.AddShader(StockShaders.VsDefault);
            r.Shaders.AddShader(StockShaders.VsDefaultInstanced);
            r.Shaders.AddShader(StockShaders.PsDefault);
            r.Shaders.AddShader(StockShaders.VsText);
            r.Shaders.AddShader(StockShaders.PsText);

            FontDefinition fnt = LoadMinimalFont();
            RenderItemHud hud = new RenderItemHud(fnt);
            r.AddRenderItem(hud);
            
            ModelConst mc = new ModelConst();
            mc.model = Matrix4x4.Identity;
            r.SetConstant(StockShaders.Name_ModelConst, mc);

            Vector3 camera = new Vector3(1.0f, 2.0f, 3.0f);
            Vector3 at = new Vector3(0, 0, 0);
            Vector3 up = new Vector3(0, 1, 0);
            ViewProjConst vpc = new ViewProjConst();
            vpc.viewProj = Matrix4x4.Transpose(Matrix4x4.CreateLookAtLeftHanded(camera, at, up) *
               Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 3, 1, 0.01f, 100.0f));

            vpc.camera = new Vector4(camera, 1);
            r.SetConstant(StockShaders.Name_ViewProjConst, vpc);

            CameraLightConst clp = new CameraLightConst();
            clp.cameraPos = camera;
            clp.lightPos = camera;
            r.SetConstant(StockShaders.Name_CameraLightConst, clp);

            PshConst pc = new PshConst();
            pc.color = new Vector4(0, 1, 0, 1);
            pc.ambStrength = 0.2f;
            pc.flags = 0;
            r.SetConstant(StockShaders.Name_PshConst, pc);

            r.RasterFormat = new RasterInfo(256, 256, ChannelFormat.Bgra8);

            Console.WriteLine("Using " + r.DeviceName);

            return (r, hud);
        }

        [TestMethod]
        public void FontDefinitionLoadTest()
        {
            FontDefinition fnt = LoadMinimalFont();

            Assert.AreEqual("segoe-ui-minimal.png", fnt.BitmapFileName);
            Assert.AreEqual("Segoe UI", fnt.FaceName);
            Assert.AreEqual(48, fnt.FontSize);
            Assert.AreEqual(512, fnt.BitmapWidth);
            Assert.AreEqual(512, fnt.BitmapHeight);
            //Assert.AreEqual(72, fnt.LineHeight);
        }

        [TestMethod]
        public void MeasureLineTest()
        {
            FontDefinition fnt = LoadMinimalFont();
            string text = "Romulans have no honor!";

            float size10 = TextBufferGenerator.MeasureLineWidth(fnt, 10, text);
            float size20 = TextBufferGenerator.MeasureLineWidth(fnt, 20, text);

            Console.WriteLine($"size10:{size10}, size20:{size20}");

            Assert.IsTrue(size10 > 0);
            Assert.IsTrue(MathF.Abs(size20 / size10 - 2) < 1e-4f);
        }

        [TestMethod]
        public void SingleLineHudTest()
        {
            (HeadlessRenderer rend, RenderItemHud hud) = CreateRendererHud();

            hud.SetSubText(0, "XXSmall", 8, Color.Red, new RectangleF(4, 4, 256, 256));
            hud.SetSubText(1, "XSmall", 12, Color.Red, new RectangleF(4, 14, 256, 256));
            hud.SetSubText(2, "Small", 16, Color.Red, new RectangleF(4, 28, 256, 256));
            hud.SetSubText(3, "Medium", 20, Color.Red, new RectangleF(4, 46, 256, 256));
            hud.SetSubText(4, "Large", 24, Color.Red, new RectangleF(4, 68, 256, 256));
            hud.SetSubText(5, "XLarge", 32, Color.Red, new RectangleF(4, 94, 256, 256));
            hud.SetSubText(6, "XXLarge", 48, Color.Red, new RectangleF(4, 128, 256, 256));

            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("SingleLineHudTest_0.png", bmp);
        }

       [TestMethod]
        public void MultiLineHudTest()
        {
            (HeadlessRenderer rend, RenderItemHud hud) = CreateRendererHud();

            string text = @"Cannon to right of them,
Cannon to left of them,
Cannon in front of them
Volleyed and thundered;
Stormed at with shot and shell,
Boldly they rode and well,
Into the jaws of Death,
Into the mouth of hell
Rode the six hundred.";

            hud.SetSubText(0, text, 12, Color.Green, new RectangleF(4, 4, 128, 128));
            

            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("MultiLineHudTest_0.png", bmp);
        }
    }
}
