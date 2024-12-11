using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Warp9.Data;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Test
{
    [TestClass]
    public class NativeTest
    {
        private static void AssertMatrixEqual(Matrix4x4 expected, Matrix4x4 got, float tol = 1e-6f)
        {
            Matrix4x4 d = expected - got;

            if (d.FrobeniusNorm() > tol)
            {
                Console.WriteLine("Wanted: " + expected.ToString());
                Console.WriteLine("Got   : " + got.ToString());
                Assert.Fail(string.Format("Matrices are not equal within {0}.", tol));
            }
        }

        private static PointCloud DistortPcl(PointCloud pcl, Vector3 t, float scale, float noise)
        {
            MeshBuilder mb = pcl.ToBuilder();
            List<Vector3> pos = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);

            Random rand = new Random(74656);
            for (int i = 0; i < pos.Count; i++)
            {
                Vector3 gn = new Vector3(rand.NextSingle() * noise, rand.NextSingle() * noise, rand.NextSingle() * noise);
                pos[i] = scale * pos[i] + t + gn;
            }

            return mb.ToPointCloud();
        }

        private static void ComparePcls(PointCloud pcl1, PointCloud pcl2)
        {
            MeshView? view1 = pcl1.GetView(MeshViewKind.Pos3f);
            MeshView? view2 = pcl2.GetView(MeshViewKind.Pos3f);

            if (view1 is null || view2 is null)
                throw new InvalidOperationException();

            view1.AsTypedData(out ReadOnlySpan<Vector3> v1);
            view2.AsTypedData(out ReadOnlySpan<Vector3> v2);

            float dmin = float.MaxValue;
            float dmax = float.MinValue;
            float dsum = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float d = Vector3.Distance(v1[i], v2[i]);
                dsum += d;
                if (d < dmin) dmin = d;
                if (d > dmax) dmax = d;
            }
            float davg = dsum / v1.Length;

            Console.WriteLine(string.Format("d={0}..{1} dmean={2}",
                dmin, dmax, davg));
        }

        [TestMethod]
        public void CpdInitDefaultTest()
        {
            PointCloud pcl = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl, new CpdConfiguration());

            Assert.AreEqual(WarpCoreStatus.WCORE_OK, stat);
            Assert.IsNotNull(ctx);

            Console.WriteLine(ctx.ToString());
        }

        [TestMethod]
        public void CpdRegDefaultTest()
        {
            Mesh pcl = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            PointCloud pclTarget = DistortPcl(pcl, Vector3.Zero, 1.10f, 0.25f);

            CpdConfiguration cpdCfg = new CpdConfiguration();
            cpdCfg.UseGpu = true;
            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl, cpdCfg);
            Assert.AreEqual(WarpCoreStatus.WCORE_OK, stat);
            Assert.IsNotNull(ctx);

            WarpCoreStatus regStat = ctx.Register(pclTarget, out PointCloud? pclBent, out CpdResult result);
            Console.WriteLine(result.ToString());
            Assert.IsNotNull(pclBent);
            //Assert.AreEqual(regStat, WarpCoreStatus.WCORE_OK);
            Assert.AreEqual(pcl.VertexCount, pclBent.VertexCount);

            Console.WriteLine("Y-X:");
            ComparePcls(pcl, pclTarget);
            Console.WriteLine();
            Console.WriteLine("T-X:");
            ComparePcls(pclBent, pclTarget);
            Console.WriteLine();
            Console.WriteLine("T-Y:");
            ComparePcls(pclBent, pcl);

            TestUtils.Render("CpdRegDefaultTest_0.png",
                (pcl.ToPointCloud(), Color.Red),
                (pclTarget, Color.Green),
                (pclBent, Color.White));
        }

        [TestMethod]
        public void GpaTeapotsTest()
        {
            Mesh pcl1 = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            PointCloud pcl2 = DistortPcl(pcl1, new Vector3(0.5f, 0.2f, -0.1f), 0.80f, 0.25f);
            PointCloud pcl3 = DistortPcl(pcl1, Vector3.Zero, 1.10f, 0.1f);

            PointCloud[] pcls = new PointCloud[3] { pcl1, pcl2, pcl3 };

            Gpa gpa = Gpa.Fit(pcls);
            Console.WriteLine(gpa.ToString());

            TestUtils.Render("GpaTeapotsTest_0.png",
               (gpa.Mean, Color.White),
               (gpa.GetTransformed(0), Color.DarkRed),
               (gpa.GetTransformed(1), Color.DarkGreen),
               (gpa.GetTransformed(2), Color.DarkBlue));
        }

        static void TrigridRaycastTestCase(string referenceFileName, int gridCells, int bitmapSize)
        {
            Mesh mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            SearchContext.TryInitTrigrid(mesh, gridCells, out SearchContext? ctx);
            Assert.IsNotNull(ctx);

            Vector3 camera = new Vector3(2.0f, 3.5f, 0.5f);

            TestUtils.GenerateRays(camera, bitmapSize, bitmapSize, out Vector3[] p0, out Vector3[] d);
            int n = bitmapSize * bitmapSize;
            int[] hit = new int[n];
            float[] t = new float[n];

            for (int i = 0; i < n; i++)
                p0[i] += 1.5f * camera;

            DateTime t0 = DateTime.Now;
            ctx.RaycastAos(p0.AsSpan(), d.AsSpan(), n, hit.AsSpan(), t.AsSpan());
            DateTime t1 = DateTime.Now;
            double seconds = (t1 - t0).TotalSeconds;

            Console.WriteLine(string.Format("{0} rays in {1:F3} seconds, {2:F1} rays per second",
                bitmapSize * bitmapSize, seconds, bitmapSize * bitmapSize / seconds));

            Bitmap bmp = TestUtils.RenderAsHeatmap(bitmapSize, bitmapSize, 6, 10,
               (i, j) => t[j * bitmapSize + i]);

            BitmapAsserts.AssertEqual(referenceFileName, bmp);

            ctx.Dispose();
        }

        [TestMethod]
        public void Trigrid16RaycastTest()
        {
            TrigridRaycastTestCase("TrigridRaycast16Test_0.png", 16, 128);
        }

        [TestMethod]
        public void Trigrid1RaycastTest()
        {
            TrigridRaycastTestCase("TrigridRaycast1Test_0.png", 1, 128);
        }

        public void TrigridNnTestCase(string referenceFileName, int gridCells, int bitmapSize)
        {
            Mesh mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            SearchContext.TryInitTrigrid(mesh, gridCells, out SearchContext? ctx);
            Assert.IsNotNull(ctx);

            // x0=<-3, 0, -2>, x1=<3.434, 3.15, 2>, center=<0.053937342, 1.7241387, -0.00024491842>, cs=2.0256174
            TestUtils.GenerateGrid(bitmapSize, bitmapSize,
                new Vector3(-3.5f, 5.2f, 0f), new Vector3(3.5f, 5.2f, 0f), new Vector3(-3.5f, -1.8f, 0f),
                out Vector3[] pts);

            int[] hit = new int[bitmapSize * bitmapSize];
            ResultInfoDPtBary[] res = new ResultInfoDPtBary[bitmapSize * bitmapSize];

            Stopwatch sw = new Stopwatch();
            sw.Start();
            ctx.NearestAos(pts.AsSpan(), bitmapSize * bitmapSize, 1.0f, hit.AsSpan(), res.AsSpan());
           // Parallel.For(0, bitmapSize, (i) =>
           // {
            //    ctx.NearestAos(pts.AsSpan(i * bitmapSize), bitmapSize, 1.0f, hit.AsSpan(i * bitmapSize), res.AsSpan(i * bitmapSize));
            //});
            sw.Stop();

            Console.WriteLine("{0:F1} queries per second",
                bitmapSize * bitmapSize / sw.Elapsed.TotalSeconds);

            Bitmap bmp = TestUtils.RenderAsHeatmap(bitmapSize, bitmapSize, 0, 2,
                (i, j) => MathF.Sqrt(res[j * bitmapSize + i].d));

            BitmapAsserts.AssertEqual(referenceFileName, bmp);

            ctx.Dispose();
        }

        [TestMethod]
        public void Trigrid16NnTest()
        {
            TrigridNnTestCase("Trigrid16NnTest_0.png", 16, 128);
        }

        [TestMethod]
        public void Trigrid1NnTest()
        {
            TrigridNnTestCase("Trigrid1NnTest_0.png", 1, 128);
        }

        [TestMethod]
        public void PclStatsTest()
        {
            Mesh mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            PclStat3 stat = RigidTransform.MakePclStats(mesh);

            Console.WriteLine(string.Format("x0={0}, x1={1}, center={2}, cs={3}",
                stat.x0.ToString(), stat.x1.ToString(), stat.center.ToString(), stat.size));
        }

        [TestMethod]
        public void RigidToMatrixTest()
        {
            AssertMatrixEqual(Matrix4x4.Identity, Rigid3.Identity.ToMatrix());

            Rigid3 scale2 = new Rigid3() { offset = Vector3.Zero, cs = 0.5f, rot0 = Vector3.UnitX, rot1 = Vector3.UnitY, rot2 = Vector3.UnitZ };
            AssertMatrixEqual(Matrix4x4.CreateScale(2.0f), scale2.ToMatrix());

            Rigid3 shift124 = new Rigid3() { offset = new Vector3(-1,-2,-4), cs = 1, rot0 = Vector3.UnitX, rot1 = Vector3.UnitY, rot2 = Vector3.UnitZ };
            AssertMatrixEqual(Matrix4x4.CreateTranslation(1,2,4), shift124.ToMatrix());

            // TODO: more complex cases
        }
    }
}
