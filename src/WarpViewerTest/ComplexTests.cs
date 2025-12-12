using Microsoft.VisualBasic.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Warp9.Data;
using Warp9.Viewer;

namespace Warp9.Test
{
    [TestClass]
    public class ComplexTests
    {
        public static HeadlessRenderer CreateRenderer()
        {
            Assert.IsTrue(HeadlessRenderer.TryCreate(0, out HeadlessRenderer? r));
            Assert.IsNotNull(r);

            r.CanvasColor = Color.Black;

            r.Shaders.AddShaders(StockShaders.AllShaders);

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
            renderItemMesh.FillColor = Color.LimeGreen;
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotPhongTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderTeapotColorArrayF32Test()
        {
            HeadlessRenderer rend = CreateRenderer();

            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            int nv = teapot.VertexCount;
            MeshBuilder mb = teapot.ToBuilder();

            List<Vector4> color = mb.GetSegmentForEditing<Vector4>(MeshSegmentSemantic.Color, false).Data;
            for (int i = 0; i < nv; i++)
            {
                float p = (float)i / nv;
                color.Add(new Vector4(MathF.Sin(p * 10) * 0.5f + 0.5f, p, MathF.Cos(p * 34) * 0.5f + 0.5f, 1));
            }

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = mb.ToMesh();
            renderItemMesh.Lut = Lut.Create(256, Lut.ViridisColors);
            renderItemMesh.Style = MeshRenderStyle.ColorArray;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotColorArrayF32Test_0.png", bmp);
        }

        [TestMethod]
        public void RenderTeapotColorArrayI8Test()
        {
            HeadlessRenderer rend = CreateRenderer();

            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            int nv = teapot.VertexCount;
            MeshBuilder mb = teapot.ToBuilder();

            List<uint> color = mb.GetSegmentForEditing<uint>(MeshSegmentSemantic.Color, false).Data;
            for (int i = 0; i < nv; i++)
            {
                if (i > 3000)
                    color.Add(0xff0000ffu);
                else if (i > 2000)
                    color.Add(0xff00ff00u);
                else if (i > 1000)
                    color.Add(0xffff0000u);
                else
                    color.Add(0xff808080u);
            }

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = mb.ToMesh();
            renderItemMesh.Lut = Lut.Create(256, Lut.ViridisColors);
            renderItemMesh.Style = MeshRenderStyle.ColorArray;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotColorArrayI8Test_0.png", bmp);
        }

        [TestMethod]
        public void RenderTeapotDynamicTest()
        {
            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = teapot;
            renderItemMesh.Lut = Lut.Create(256, Lut.ViridisColors);
            renderItemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn | MeshRenderStyle.EstimateNormals;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            renderItemMesh.FillColor = Color.Orange;
            renderItemMesh.UseDynamicArrays = true;
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present(); // this should look like RenderTeapotPhongTest but orange, we're not interested in this result
            
            if(!teapot.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos))
                Assert.Fail("Cannot get typed pos array.");

            int nv = pos.Length;
            Vector3[] pos2 = new Vector3[nv];
           
            for (int i = 0; i < nv; i++)
                pos2[i] = pos[i] * 1.25f;

            renderItemMesh.UpdateData(new BufferSegment<Vector3>(pos2), MeshSegmentSemantic.Position);
            rend.Present(); // now the teapot should appear larger

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotDynamicTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderTeapotPointsTest()
        {
            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            renderItemMesh.Lut = Lut.Create(256, Lut.ViridisColors);
            renderItemMesh.Style = MeshRenderStyle.ColorFlat;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            renderItemMesh.PointWireColor = Color.White;
            renderItemMesh.RenderFace = false;
            renderItemMesh.RenderPoints = true;
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotPointsTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderTeapotWithPointTest()
        {
            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            renderItemMesh.Lut = Lut.Create(256, Lut.ViridisColors);
            renderItemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn | MeshRenderStyle.EstimateNormals;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            renderItemMesh.FillColor = Color.DimGray;
            renderItemMesh.PointWireColor = Color.Red;
            renderItemMesh.RenderFace = true;
            renderItemMesh.RenderPoints = true;
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotWithPointTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderTeapotPhongScalarFieldValueTest()
        {
            Mesh m = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            float[] v = new float[m.VertexCount];
            if (!m.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos))
                Assert.Fail();

            for (int i = 0; i < m.VertexCount; i++)
                v[i] = MathF.Abs(Vector3.Dot(Vector3.Normalize(pos[i]), Vector3.UnitY));

            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = m;
            renderItemMesh.Lut = Lut.Create(256, Lut.ViridisColors);
            renderItemMesh.Style = MeshRenderStyle.ColorLut | MeshRenderStyle.PhongBlinn | MeshRenderStyle.EstimateNormals | MeshRenderStyle.ShowValueLevel;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            renderItemMesh.LevelValue = 0.65f;
            renderItemMesh.FillColor = Color.Red;
            renderItemMesh.SetValueField(new BufferSegment<float>(v));
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotPhongScalarFieldValueTest_0.png", bmp);
        }


        /*[TestMethod]
        public void RenderTeapotPhongWithLandmarksTest()
        {
            HeadlessRenderer rend = CreateRenderer();
            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = teapot;
            renderItemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn | MeshRenderStyle.EstimateNormals;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);
            renderItemMesh.FillColor = Color.Gray;
            rend.AddRenderItem(renderItemMesh);

            RenderItemInstancedMesh renderItemLm = new RenderItemInstancedMesh();
            renderItemLm.Mesh = TestUtils.MakeCubeIndexed(0.2f);
            renderItemLm.Instances = TestUtils.SelectIndices(teapot, (i) => i % 100 == 0);
            renderItemLm.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn | MeshRenderStyle.EstimateNormals;
            renderItemLm.BaseModelMatrix = renderItemMesh.ModelMatrix;
            renderItemLm.FillColor = Color.Red;
            rend.AddRenderItem(renderItemLm);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderTeapotPhongWithLandmarksTest_0.png", bmp);
        }*/


        [TestMethod]
        public void RenderSuzannePhongTest()
        {
            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = TestUtils.LoadObjAsset("suzanne.obj", IO.ObjImportMode.AllUnshared);
            renderItemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateScale(1.5f);
            renderItemMesh.FillColor = Color.Gray;
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderSuzannePhongTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderSuzanneDiffuseTest()
        {
            HeadlessRenderer rend = CreateRenderer();

            RenderItemMesh renderItemMesh = new RenderItemMesh();
            renderItemMesh.Mesh = TestUtils.LoadObjAsset("suzanne.obj", IO.ObjImportMode.AllUnshared);
            renderItemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.DiffuseLighting;
            renderItemMesh.ModelMatrix = Matrix4x4.CreateScale(1.5f);
            renderItemMesh.FillColor = Color.Gray;
            rend.AddRenderItem(renderItemMesh);

            rend.CanvasColor = Color.Black;
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderSuzanneDiffuseTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderGridTest()
        {
            HeadlessRenderer rend = CreateRenderer();
            RenderItemGrid renderItemGrid = new RenderItemGrid();
            rend.AddRenderItem(renderItemGrid);

            rend.CanvasColor = Color.FromArgb(52, 52, 52);
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderGridTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderIcosahedronTest()
        {
            HeadlessRenderer rend = CreateRenderer();
            RenderItemMesh ri = new RenderItemMesh();
            ri.Mesh = MeshUtils.MakeIcosahedron(0.7f);
            ri.Style = MeshRenderStyle.PhongBlinn | MeshRenderStyle.ColorFlat | MeshRenderStyle.EstimateNormals;
            ri.FillColor = Color.Lime;
            rend.AddRenderItem(ri);

            rend.CanvasColor = Color.FromArgb(52, 52, 52);
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderIcosahedronTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderInstancedIcosahedronTest()
        {
            HeadlessRenderer rend = CreateRenderer();
            RenderItemInstancedMesh ri = new RenderItemInstancedMesh();
            ri.Mesh = MeshUtils.MakeIcosahedron();
            ri.InstanceScale = 0.1f;
            ri.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals;
            ri.FillColor = System.Drawing.Color.Lime;
            ri.Instances = ri.Mesh;
            rend.AddRenderItem(ri);
            
            rend.CanvasColor = Color.FromArgb(52, 52, 52);
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderInstancedIcosahedronTest_0.png", bmp);
        }

        [TestMethod]
        public void RenderIcosahedronInstanceColorTest()
        {
            MeshBuilder mb = new MeshBuilder();
            List<Vector3> instPos = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
            instPos.Add(new Vector3(-1f, -1f, 0));
            instPos.Add(new Vector3(1f, -1f, 0));
            instPos.Add(new Vector3(-1f, 1f, 0));
            instPos.Add(new Vector3(1f, 1f, 0));
            instPos.Add(new Vector3(0.0f, 0.0f, 0));

            List<uint> instColor = mb.GetSegmentForEditing<uint>(MeshSegmentSemantic.Color, false).Data;
            instColor.Add(0xff0000ff);
            instColor.Add(0xff00ff00);
            instColor.Add(0xffff0000);
            instColor.Add(0xffffffff);
            instColor.Add(0xff808080);

            HeadlessRenderer rend = CreateRenderer();           
            RenderItemInstancedMesh ri = new RenderItemInstancedMesh();
            ri.Mesh = MeshUtils.MakeIcosahedron();
            ri.InstanceScale = 0.3f;
            ri.Style = MeshRenderStyle.ColorArray | MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals;
            ri.Instances = mb.ToPointCloud();
            ri.UseInstanceColor = true;
            rend.AddRenderItem(ri);

            rend.CanvasColor = Color.FromArgb(52, 52, 52);
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("RenderIcosahedronInstanceColorTest_0.png", bmp);
        }

        [TestMethod]
        public void InstancedDynamicNoInitTest()
        {
            MeshBuilder mb = new MeshBuilder();
            List<Vector3> instPos = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
            instPos.Add(new Vector3(-1f, -1f, 0));
            instPos.Add(new Vector3(1f, -1f, 0));
            instPos.Add(new Vector3(-1f, 1f, 0));
            instPos.Add(new Vector3(1f, 1f, 0));
            instPos.Add(new Vector3(0.0f, 0.0f, 0));

            uint[] dynColor = new uint[] { 0xff0000ff, 0xff00ff00, 0xffff0000, 0xffffffff, 0xff808080 };

            HeadlessRenderer rend = CreateRenderer();
            RenderItemInstancedMesh ri = new RenderItemInstancedMesh();
            ri.Mesh = MeshUtils.MakeIcosahedron();
            ri.InstanceScale = 0.3f;
            ri.Style = MeshRenderStyle.ColorArray | MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals;
            ri.Instances = mb.ToPointCloud();
            ri.UseInstanceColor = true;            
            rend.AddRenderItem(ri);
            
            rend.CanvasColor = Color.FromArgb(52, 52, 52);
            rend.Present();

            ri.UpdateInstanceData(new BufferSegment<uint>(dynColor), MeshSegmentSemantic.Color);
            rend.Present();

            using (Bitmap bmp = rend.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("InstancedDynamicNoInitTest_0.png", bmp);
        }
    }
}
