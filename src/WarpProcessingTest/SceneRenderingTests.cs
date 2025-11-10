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
            r.Shaders.AddShaders(StockShaders.AllShaders);
            r.RasterFormat = new RasterInfo(128, 128);

            Console.WriteLine("Using " + r.DeviceName);

            return r;
        }

        [TestMethod]
        public void BasicDynamicSceneTest()
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

            Console.WriteLine("scene: " + scene.ToString());
            Console.WriteLine("rend : " + vsw.ToString());
            Console.WriteLine();

            renderer.Present();
            using (Bitmap bmp = renderer.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("BasicDynamicSceneTest_0.png", bmp, true);

            Console.WriteLine("scene: " + scene.ToString());
            Console.WriteLine("rend : " + vsw.ToString());
            Console.WriteLine();

            scene.Mesh0.FlatColor = Color.YellowGreen;

            renderer.Present();
            using (Bitmap bmp = renderer.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("BasicDynamicSceneTest_1.png", bmp, true);

            Console.WriteLine("scene: " + scene.ToString());
            Console.WriteLine("rend : " + vsw.ToString());
            Console.WriteLine();

            Assert.IsTrue(proj.TryGetReference(teapotKey, out Mesh? teapot) && teapot is not null);
            teapot.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos);
            int n = pos.Length;
            Vector3[] pos2 = new Vector3[n];
            for (int i = 0; i < n; i++)
                pos2[i] = 1.25f * pos[i];
            scene.Mesh0.PositionOverride = new ReferencedData<BufferSegment<Vector3>>(new BufferSegment<Vector3>(pos2));

            Console.WriteLine("scene: " + scene.ToString());
            Console.WriteLine("rend : " + vsw.ToString());
            Console.WriteLine();

            renderer.Present();
            using (Bitmap bmp = renderer.ExtractColorAsBitmap())
                BitmapAsserts.AssertEqual("BasicDynamicSceneTest_2.png", bmp, true);

            Console.WriteLine("scene: " + scene.ToString());
            Console.WriteLine("rend : " + vsw.ToString());
            Console.WriteLine();
        }
    }
}
