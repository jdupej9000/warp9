using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Viewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Warp9.Data;

namespace Warp9.Test
{
    [TestClass]
    public class ComplexTests
    {
        private static HeadlessRenderer CreateRenderer()
        {
            Assert.IsTrue(HeadlessRenderer.TryCreate(0, out HeadlessRenderer? r));
            Assert.IsNotNull(r);

            r.CanvasColor = Color.Black;

            r.Shaders.AddShader(StockShaders.VsDefault);
            r.Shaders.AddShader(StockShaders.VsDefaultInstanced);
            r.Shaders.AddShader(StockShaders.PsDefault);

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

            return r;
        }

        [TestMethod]
        public void RenderTeapotPhongTest()
        {
            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            renderItemMesh.Lut = Lut.Create(256, Lut.ViridisColors);
            renderItemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn | MeshRenderStyle.EstimateNormals;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            renderItemMesh.Color = Color.LimeGreen;
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotPhongTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderTeapotPhongScalarFieldValueTest()
        {
            Mesh m = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            float[] v = new float[m.VertexCount];
            MeshView? posView = m.GetView(MeshViewKind.Pos3f);
            Assert.IsNotNull(posView);
            Assert.IsTrue(posView.AsTypedData(out ReadOnlySpan<Vector3> pos));

            for (int i = 0; i < m.VertexCount; i++)
                v[i] = MathF.Abs(Vector3.Dot(Vector3.Normalize(pos[i]), Vector3.UnitY));

            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = m;
            renderItemMesh.Lut = Lut.Create(256, Lut.ViridisColors);
            renderItemMesh.Style = MeshRenderStyle.ColorLut | MeshRenderStyle.PhongBlinn | MeshRenderStyle.EstimateNormals | MeshRenderStyle.ShowValueLevel;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            renderItemMesh.LevelValue = 0.65f;
            renderItemMesh.Color = Color.Red;
            renderItemMesh.SetValueField(v);
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotPhongScalarFieldValueTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderSuzannePhongTest()
        {
            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = TestUtils.LoadObjAsset("suzanne.obj", IO.ObjImportMode.AllUnshared);
            renderItemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateScale(1.5f);
            renderItemMesh.Color = Color.Gray;
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderSuzannePhongTest_0.png", bmp);
        }
    }
}