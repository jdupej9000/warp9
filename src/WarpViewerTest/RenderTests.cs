﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Numerics;
using Warp9.Viewer;

namespace Warp9.Test
{
    [TestClass]
    public class RenderTests
    {
        private static (HeadlessRenderer, RenderItemCube?) CreateRenderer(bool addCube = true)
        {
            int adapter = TestUtils.FindAdapter();

            Assert.IsTrue(HeadlessRenderer.TryCreate(adapter, out HeadlessRenderer? r));
            Assert.IsNotNull(r);
         
            r.CanvasColor = Color.Black;

            r.Shaders.AddShader(StockShaders.VsDefault);
            r.Shaders.AddShader(StockShaders.VsDefaultInstanced);
            r.Shaders.AddShader(StockShaders.PsDefault);
            r.Shaders.AddShader(RenderItemCube.GsExplode);

            RenderItemCube? cube = null;
            if (addCube)
            {
                cube = new RenderItemCube();
                r.AddRenderItem(cube);
            }

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

            r.RasterFormat = new RasterInfo(128, 128);

            Console.WriteLine("Using " + r.DeviceName);

            return (r, cube);
        }

        [TestMethod]
        public void BlankCanvasTest()
        {
            (HeadlessRenderer rend, _) = CreateRenderer(false);

            rend.CanvasColor = Color.DarkCyan;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("BlankCanvasTest_0.png", bmp);

            rend.CanvasColor = Color.DarkRed;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("BlankCanvasTest_1.png", bmp);
        }

        [TestMethod]
        public void ColorCubeOneVbuffTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.ColorArray;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeOneVbuffTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeFlatColorTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.FlatColor;
            cube.Color = Color.Gray;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeFlatColorTest_0.png", bmp);
        }
              
        [TestMethod]
        public void ColorCubeTextureTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.Texture;
            cube.Color = Color.Gray;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeTextureTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeWireframeTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.NoFillExploded;
            cube.Color = Color.Gray;
            cube.AddWireframe = true;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeWireframeTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeWithWireframeTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.FlatColor;
            cube.Color = Color.Gray;
            cube.AddWireframe = true;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeWithWireframeTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubePhongTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.FlatColorPhong;
            cube.Color = Color.DarkOliveGreen;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubePhongTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubePhongAlphaTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.FlatColorPhong;
            cube.Color = Color.FromArgb(128, Color.DarkOliveGreen);
            cube.AlphaBlend = true;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubePhongAlphaTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeInstancedTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.FlatColorPhong;
            cube.Color = Color.DarkOliveGreen;
            cube.UseInstances = true;

            ModelConst mc = new ModelConst();
            mc.model = Matrix4x4.CreateScale(0.25f);
            rend.SetConstant(StockShaders.Name_ModelConst, mc);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeInstancedTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeIndexedInstancedTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.ColorArray;
            cube.UseInstances = true;

            ModelConst mc = new ModelConst();
            mc.model = Matrix4x4.CreateScale(0.25f);
            rend.SetConstant(StockShaders.Name_ModelConst, mc);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeIndexedInstancedTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeScaleTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.Scale;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeScaleTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeScaleWithLevelTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.Scale;
            cube.Color = Color.Black;
            cube.AddValueLevel = true;
            cube.ValueLevel = 0.7f;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeScaleWithLevelTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeScaleWithLevelScaledTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.Scale;
            cube.Color = Color.Black;
            cube.AddValueLevel = true;
            cube.ValueLevel = 0.7f;
            cube.ValueMin = 0.6f;
            cube.ValueMax = 0.8f;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeScaleWithLevelScaledTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeEstNormalsPhongTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.FlatColorPhongEstNormals;
            cube.Color = Color.DarkOliveGreen;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeEstNormalsPhongTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeScaleEstNormalsPhongTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.ScalePhongEstNormals;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorScaleCubeEstNormalsPhongTest_0.png", bmp);
        }
    }
}
