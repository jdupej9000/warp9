using System;
using System.IO;
using Warp9.Data;
using Warp9.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Numerics;
using Warp9.Viewer;
using Microsoft.VisualBasic.Logging;

namespace Warp9.Test
{
    public static class TestUtils
    {
        public static readonly string AssetsPath = @"../../test/data/";

        public static int FindAdapter()
        {
            var adapters = RendererBase.EnumAdapters();
            int adapterIdx = 0;

            foreach (var kvp in adapters)
            {
                if (kvp.Value.Contains("nvidia", StringComparison.InvariantCultureIgnoreCase))
                {
                    adapterIdx = kvp.Key;
                    break;
                }
            }

            Console.WriteLine(string.Format("Selecting graphics adapter {0}: {1}", adapterIdx, adapters[adapterIdx]));
            return adapterIdx;
        }

        public static Stream OpenAsset(string name)
        {
            string path = Path.Combine(AssetsPath, name);

            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public static Mesh LoadObjAsset(string name, ObjImportMode mode)
        {
            using Stream s = TestUtils.OpenAsset(name);
            if (!ObjImport.TryImport(s, mode, out Mesh m, out string errMsg))
                Assert.Inconclusive("Failed to load OBJ asset: " + errMsg);

            return m;
        }

        private static HeadlessRenderer CreateRenderer()
        {
            int adapterIdx = FindAdapter();
            Assert.IsTrue(HeadlessRenderer.TryCreate(adapterIdx, out HeadlessRenderer? r));
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

            r.RasterFormat = new RasterInfo(512, 512);

            return r;
        }

        public static void Render(string fileName, params (PointCloud, Color)[] items)
        {
            HeadlessRenderer rend = CreateRenderer();

            foreach (var i in items)
            {
                RenderItemMesh renderItemMesh = new RenderItemMesh();
                renderItemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);

                if (i.Item1 is Mesh m)
                {
                    renderItemMesh.Mesh = m;
                    renderItemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn | MeshRenderStyle.EstimateNormals;
                }
                else if (i.Item1 is PointCloud p)
                {
                    renderItemMesh.Mesh = Mesh.FromPointCloud(p);
                    renderItemMesh.Style = MeshRenderStyle.ColorFlat;
                    renderItemMesh.RenderAsPoints = true;
                }

                renderItemMesh.Color = i.Item2;
                rend.AddRenderItem(renderItemMesh);


                rend.CanvasColor = Color.Black;
                rend.Present();

                Directory.CreateDirectory(Path.GetFullPath(BitmapAsserts.ResultPath));
                using (Bitmap bmp = rend.ExtractColorAsBitmap())
                {
                    bmp.Save(Path.GetFullPath(Path.Combine(BitmapAsserts.ResultPath, fileName)));
                }

            }
        }

        public static void GenerateGrid(int nx, int ny, Vector3 p00, Vector3 p01, Vector3 p10, out Vector3[] p)
        {
            p = new Vector3[nx * ny];

            Vector3 dx = (p01 - p00) / nx;
            Vector3 dy = (p10 - p00) / nx;

            for (int j = 0; j < ny; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    p[i + nx * j] = p00 + (float)i * dx + (float)j * dy;
                }
            }
        }

        public static void GenerateRays(Vector3 camera, int nx, int ny, out Vector3[] p0, out Vector3[] d)
        {
            // TODO: make the constants more global
            // https://www.mvps.org/directx/articles/rayproj.htm
            p0 = new Vector3[nx * ny];
            d = new Vector3[nx * ny];

            const float Fov = MathF.PI * 0.95f;
            const float Aspect = 1;
            const float Far = 100.0f;
            const float Near = 0.01f;
            //Vector3 camera = new Vector3(0.75f, 2.0f, 2.5f);
            //Vector3 camera = new Vector3(1.0f, 2.0f, -3.0f);
            Vector3 at = new Vector3(0, 0, 0);
            Vector3 up = new Vector3(0, 1, 0);

            Matrix4x4 viewProj = Matrix4x4.CreateLookAtLeftHanded(camera, at, up) *
               Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Fov, Aspect, Near, Far);

            //Matrix4x4 viewProj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Fov, Aspect, Near, Far) * Matrix4x4.CreateLookAtLeftHanded(camera, at, up);

            Matrix4x4.Invert(viewProj, out Matrix4x4 viewProjInv);

            float wd2r = 1.0f / (nx / 2.0f);
            float hd2r = 1.0f / (ny / 2.0f);

            for (int j = 0; j < ny; j++)
            {
                float dy = MathF.Tan(Fov * 0.5f) * (1.0f - j * hd2r);

                for (int i = 0; i < nx; i++)
                {
                    float dx = MathF.Tan(Fov * 0.5f) * (i * wd2r - 1.0f) / Aspect;

                    Vector3 pp1 = Vector3.Transform(new Vector3(dx * Near, dy * Near, Near), viewProjInv);
                    Vector3 pp2 = Vector3.Transform(new Vector3(dx * Far, dy * Far, Far), viewProjInv);

                    int idx = j * nx + i;
                    p0[idx] = camera;
                    d[idx] = Vector3.Normalize(pp2 - pp1);
                }
            }
        }
    }
}
