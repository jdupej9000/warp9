using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Model;
using Warp9.Scene;
using Warp9.Viewer;

namespace Warp9.Test
{
    [TestClass]
    public class SceneRenderingTests
    {
        private static Project MakeProject(out long teapotKey)
        {
            Project project = Project.CreateEmpty();

            Mesh m = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            teapotKey = project.AddReferenceDirect("teapot.obj", ProjectReferenceFormat.ObjMesh, m);

            return project;
        }

        private static HeadlessRenderer CreateRenderer()
        {
            int adapter = TestUtils.FindAdapter();

            Assert.IsTrue(HeadlessRenderer.TryCreate(adapter, out HeadlessRenderer? r));
            Assert.IsNotNull(r);

            r.CanvasColor = Color.Black;
            r.Shaders.AddShader(StockShaders.VsDefault);
            r.Shaders.AddShader(StockShaders.VsDefaultInstanced);
            r.Shaders.AddShader(StockShaders.PsDefault);            
            r.RasterFormat = new RasterInfo(128, 128);

            Console.WriteLine("Using " + r.DeviceName);

            return r;
        }

        [TestMethod]
        public void BasicSceneTest()
        {
            Project proj = MakeProject(out long teapotKey);

            HeadlessRenderer renderer = CreateRenderer();
            ViewerSceneRenderer vsw = new ViewerSceneRenderer(proj);
            ViewerScene scene = new ViewerScene();
            vsw.Scene = scene;

            scene.Viewport = new Size(128, 128);
            Vector3 camera = new Vector3(2.0f, 4.0f, 6.0f);
            Vector3 at = new Vector3(0, 0, 0);
            Vector3 up = new Vector3(0, 1, 0);
            scene.ViewMatrix = Matrix4x4.CreateLookAtLeftHanded(camera, at, up);
            scene.Mesh0 = new MeshSceneElement();
            scene.Mesh0.Mesh = new ReferencedData<Mesh>(teapotKey);
            scene.Mesh0.Flags = MeshRenderFlags.Fill | MeshRenderFlags.EstimateNormals | MeshRenderFlags.Diffuse;

            vsw.AttachToRenderer(renderer);

            renderer.Present();
            using (Bitmap bmp = renderer.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("BasicSceneTest_0.png", bmp);
        
        }
    }
}
