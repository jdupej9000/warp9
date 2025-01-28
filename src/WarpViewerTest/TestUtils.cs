using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.Diagnostics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using Warp9.Data;
using Warp9.IO;
using Warp9.Viewer;

namespace Warp9.Test
{
    public enum TriStyle
    {
        PointCloud,
        MeshFilled,
        MeshWire,
        Landmarks
    }

    public class TestRenderItem
    {
        public TestRenderItem(TriStyle style, object data, Color? col = null, Color? wireCol = null, MeshRenderStyle? mrs = null, float lmScale=0.1f)
        {
            Data = data;
            Style = style;
            Color = col ?? Color.Gray;
            WireColor = wireCol ?? Color.White;
            LandmarkScale = lmScale;

            MeshStyle = mrs ?? style switch
            {
                TriStyle.PointCloud => MeshRenderStyle.ColorFlat,
                TriStyle.MeshFilled => MeshRenderStyle.ColorFlat | MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals,
                TriStyle.MeshWire => MeshRenderStyle.ColorFlat,
                TriStyle.Landmarks => MeshRenderStyle.ColorFlat | MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals,
                _ => throw new ArgumentException()
            };
        }

        object Data;
        float LandmarkScale;
        TriStyle Style;
        Color Color, WireColor;
        MeshRenderStyle MeshStyle;

        public RenderItemBase ToRenderItem()
        {
            if (Style == TriStyle.PointCloud || Style == TriStyle.MeshFilled || Style == TriStyle.MeshWire)
            {
                RenderItemMesh ri = new RenderItemMesh();

                if (Data is Mesh m)
                {
                    ri.Mesh = m;
                }
                else if (Data is PointCloud pcl)
                {
                    if (Style != TriStyle.PointCloud)
                        throw new InvalidOperationException();

                    ri.Mesh = Mesh.FromPointCloud(pcl);
                }

                ri.RenderPoints = Style == TriStyle.PointCloud;
                ri.RenderWireframe = Style == TriStyle.MeshWire;
                ri.RenderFace = Style == TriStyle.MeshFilled;
                ri.RenderBlend = true;
                ri.RenderCull = true;
                ri.RenderDepth = true;
                ri.Style = MeshStyle;
                ri.FillColor = Color;
                ri.PointWireColor = WireColor;
                return ri;
            }
            else if (Style == TriStyle.Landmarks)
            {
                RenderItemInstancedMesh renderItemLm = new RenderItemInstancedMesh();
                renderItemLm.Mesh = TestUtils.MakeCubeIndexed(LandmarkScale);
                renderItemLm.Instances = Data as PointCloud;
                renderItemLm.Style = MeshStyle;
                renderItemLm.FillColor = Color;
                return renderItemLm;
            }

            throw new InvalidOperationException();
        }
    }


    public static class TestUtils
    {
        public static readonly string AssetsPath = @"../../test/data/";

        public static int FindAdapter(bool prefeerNvidia = true)
        {
            var adapters = RendererBase.EnumAdapters();
            int adapterIdx = 0;

            if (prefeerNvidia)
            {
                foreach (var kvp in adapters)
                {
                    if (kvp.Value.Contains("nvidia", StringComparison.InvariantCultureIgnoreCase))
                    {
                        adapterIdx = kvp.Key;
                        break;
                    }
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

        public static Bitmap RenderAsHeatmap(int width, int height, float min, float max, Func<int, int, float> fun)
        {
            Lut lut = Lut.Create(256, Lut.FastColors);
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
                        float rraw = fun(i, j);
                        if (rraw < 0)
                        {
                            ptrSpan[i] = 0;
                        }
                        else
                        {
                            float r = (rraw - min) / (max - min);
                            ptrSpan[i] = lut.Sample(r).ToArgb();
                        }
                    }
                }

                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }

        public static void LoadBitmapAsFloatGrey(string assetFileName, out float[] data, out int height, out int width)
        {
            Bitmap bmp = new Bitmap(Path.Combine(AssetsPath, assetFileName));
            height = bmp.Height;
            width = bmp.Width;
            data = new float[height * width];

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            const float norm = 1.0f / 255.0f;

            unsafe
            {
                int p = 0;
                for (int j = 0; j < height; j++)
                {
                    uint* d = (uint*)(bmpData.Scan0 + j * bmpData.Stride);
                    for (int i = 0; i < width; i++)
                    {
                        float grey = 0.299f * ((d[i] >> 16) & 0xff) +
                            0.587f * ((d[i] >> 8) & 0xff) +
                            0.114f * ((d[i] >> 0) & 0xff);
                        data[p++] = grey * norm;
                    }
                }
            }

            bmp.UnlockBits(bmpData);
        }

        public static HeadlessRenderer CreateRenderer(bool preferNvidia=true)
        {
            int adapterIdx = FindAdapter(preferNvidia);
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

        public static string MakeResultPath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(BitmapAsserts.ResultPath, fileName));
        }

        public static void Render(string fileName, params TestRenderItem[] items)
        {
            Render(CreateRenderer(false), fileName, Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f), items);
        }

        public static void Render(HeadlessRenderer rend, string fileName, Matrix4x4 modelMat, params TestRenderItem[] items)
        {
            rend.ClearRenderItems();
            foreach (var i in items)
            {
                RenderItemBase rib = i.ToRenderItem();
                if (rib is RenderItemMesh rim)
                    rim.ModelMatrix = modelMat;
                else if (rib is RenderItemInstancedMesh riim)
                    riim.BaseModelMatrix = modelMat;
               
                rend.AddRenderItem(rib);
            }

            rend.CanvasColor = Color.Black;
            rend.Present();

            Directory.CreateDirectory(Path.GetFullPath(BitmapAsserts.ResultPath));
            using (Bitmap bmp = rend.ExtractColorAsBitmap())
            {
                string fullPath = MakeResultPath(fileName);
                bmp.Save(fullPath);
                Console.WriteLine("Saved " + fullPath);
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

        public static Mesh MakeCubeIndexed(float scale = 1)
        {
            float[] vb = {
            1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, 1.0f,
            -1.0f, -1.0f, 1.0f,
            -1.0f, -1.0f, -1.0f,
            1.0f, 1.0f, -1.0f,
            1.0f, 1.0f, 1.0f,
            -1.0f, 1.0f, 1.0f,
            -1.0f, 1.0f, -1.0f
            };

            int[] ib = {
                1,2,3,7,6,5,4,5,1,5,6,2,2,6,7,0,3,7,0,1,3,4,7,5,0,4,1,1,5,2,3,2,7,4,0,7
            };

            MeshBuilder mb = new MeshBuilder();
            List<Vector3> pos = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            for (int i = 0; i < vb.Length; i += 3)
                pos.Add(new Vector3(scale * vb[i], scale * vb[i + 1], scale * vb[i + 2]));

            List<FaceIndices> faces = mb.GetIndexSegmentForEditing();
            for(int i = 0; i < ib.Length; i+=3)
                faces.Add(new FaceIndices(ib[i], ib[i + 1], ib[i + 2]));

            return mb.ToMesh();
        }

        public static PointCloud SelectIndices(PointCloud pcl, Predicate<int> sel)
        {
            int nv = pcl.VertexCount;
            MeshView posView = pcl.GetView(MeshViewKind.Pos3f, true) ?? throw new InvalidOperationException();
            posView.AsTypedData(out ReadOnlySpan<Vector3> posAll);

            MeshBuilder mb = new MeshBuilder();
            List<Vector3> pos = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            for (int i = 0; i < nv; i++)
            {
                if (sel(i))
                    pos.Add(posAll[i]);
            }

            return mb.ToPointCloud();
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
