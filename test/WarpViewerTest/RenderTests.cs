using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Viewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Warp9.Test
{
    [TestClass]
    public class RenderTests
    {
        private static (HeadlessRenderer, RenderItemCube?) CreateRenderer(bool addCube = true)
        {
            Assert.IsTrue(HeadlessRenderer.TryCreate(0, out HeadlessRenderer? r));
            Assert.IsNotNull(r);
         
            r.CanvasColor = Color.Black;

            r.Shaders.AddShader(StockShaders.VsDefault);
            r.Shaders.AddShader(StockShaders.VsDefaultInstanced);
            r.Shaders.AddShader(StockShaders.PsDefault);

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

            cube.Style = CubeRenderStyle.FlatColor;
            cube.Color = Color.DarkOliveGreen;
            cube.TriangleSoup = true;

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubePhongTest_0.png", bmp);
        }

        [TestMethod]
        public void ColorCubeInstancedTest()
        {
            (HeadlessRenderer rend, RenderItemCube? cube) = CreateRenderer(true);
            Assert.IsNotNull(cube);

            cube.Style = CubeRenderStyle.FlatColor;
            cube.Color = Color.DarkOliveGreen;
            cube.TriangleSoup = true;
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
            cube.TriangleSoup = false;
            cube.UseInstances = true;

            ModelConst mc = new ModelConst();
            mc.model = Matrix4x4.CreateScale(0.25f);
            rend.SetConstant(StockShaders.Name_ModelConst, mc);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("ColorCubeIndexedInstancedTest_0.png", bmp);
        }
    }
}
