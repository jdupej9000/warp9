using SharpDX.Direct3D11;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms.Design;
using Warp9.Data;
using Warp9.Native;
using Warp9.Processing;
using Warp9.Utils;
using Warp9.Viewer;

namespace Warp9.Test
{
    [TestClass]
    public class NativeTest
    {
        private static void SetOptPath(WarpCoreOptimizationPath p)
        {
            int ret = WarpCore.set_optpath((int)p);

            if (p != WarpCoreOptimizationPath.Maximum && ret != (int)p)
                Assert.Inconclusive("This platform is incapable of executing the optimization path " + p.ToString());
        }

        private static void RestoreOptPath()
        {
            WarpCore.set_optpath((int)WarpCoreOptimizationPath.Maximum);
        }

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

        private static PointCloud TranslateTwistPcl(PointCloud pcl, Vector3 t, float twist)
        {
            MeshBuilder mb = pcl.ToBuilder();
            List<Vector3> pos = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);

            Random rand = new Random(74656);
            for (int i = 0; i < pos.Count; i++)
            {
                Vector3 pt = pos[i];

                float angle = twist * pt.X;
                float s = MathF.Sin(angle);
                float c = MathF.Cos(angle);

                Vector3 twisted = new Vector3(
                    pt.X,
                    c * pt.Y - s * pt.Z,
                    s * pt.Y + c * pt.Z);

                pos[i] = twisted + t;                
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
        public void InfoTest()
        {
            const int MaxDataLen = 1024;
            StringBuilder sb = new StringBuilder(MaxDataLen);

            foreach (WarpCoreInfoIndex idx in Enum.GetValues(typeof(WarpCoreInfoIndex)))
            {
                int len = WarpCore.wcore_get_info((int)idx, sb, MaxDataLen);
                Console.WriteLine(idx.ToString() + ": " + sb.ToString());
            }
        }


        [TestMethod]
        public void CpdInitDefaultTest()
        {
            Mesh mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            PointCloud pcl = mesh;
            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl, new CpdConfiguration());

            Assert.AreEqual(WarpCoreStatus.WCORE_OK, stat);
            Assert.IsNotNull(ctx);

            Console.WriteLine(ctx.ToString());

            int neig = ctx.NumEigenvectors;
            int m = pcl.VertexCount;
            ReadOnlySpan<float> initData = MemoryMarshal.Cast<byte, float>(ctx.NativeInitData);
            ReadOnlySpan<float> lambda = initData.Slice(0, neig);
            ReadOnlySpan<float> q = initData.Slice(m * 2);

            Console.WriteLine("Eigenvalues: " +
                string.Join(", ", lambda.ToArray().Select((l) => l.ToString())));

            Console.WriteLine("Q: " + q.Length + " Bytes.");

            TestEigenvectors(q, m, neig);

            Bitmap bmpEigenvectors = VisualizeEigenvectors(mesh, new Size(128, 128), neig, q);
            bmpEigenvectors.Save(Path.Combine(BitmapAsserts.ResultPath, "CpdInitDefaultTest_0.png"));
        }

        private static Bitmap VisualizeEigenvectors(Mesh m, Size frameSize, int neigs, ReadOnlySpan<float> d)
        {
            HeadlessRenderer rend = ComplexTests.CreateRenderer();
            RenderItemMesh rim = new RenderItemMesh();
            rim.Mesh = m;
            rim.Lut = Lut.Create(256, Lut.JetColors);
            rim.Style = MeshRenderStyle.ColorLut | MeshRenderStyle.ShowValueLevel;
            rim.ModelMatrix = Matrix4x4.CreateTranslation(-1.4f, -2.0f, -2.0f);
            rim.ValueMax = 0.02f;
            rim.ValueMin = -0.02f;
            rim.LevelValue = 0;
            rim.FillColor = Color.Black;
            rend.AddRenderItem(rim);

            int nv = m.VertexCount;

            Bitmap ret = new Bitmap(frameSize.Width, frameSize.Height * neigs, PixelFormat.Format32bppArgb);
            BitmapData bmp = ret.LockBits(new Rectangle(0, 0, frameSize.Width, frameSize.Height * neigs),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            float[] attr = new float[nv];

            unsafe
            {
                int numBytesFrame = bmp.Stride * frameSize.Height;

                for (int i = 0; i < neigs; i++)
                {
                    d.Slice(i * nv, nv).CopyTo(attr.AsSpan());
                    rim.SetValueField(attr);

                    rend.Present();
                    Span<byte> s = new Span<byte>((void*)(bmp.Scan0 + i * numBytesFrame), numBytesFrame);
                    rend.ExtractColor(s);
                }
            }

            ret.UnlockBits(bmp);
            return ret;
        }

        private static void TestEigenvectors(ReadOnlySpan<float> d, int nv, int neigs)
        {
            bool fail = false;
            for (int i = 0; i < neigs - 1; i++)
            {
                for (int j = i + 1; j < neigs; j++)
                {
                    double dot = 0;

                    for (int k = 0; k < nv; k++)
                        dot += (double)d[k + i * nv] * d[k + j * nv];

                    if (dot > 1e-6)
                    {
                        Console.WriteLine($"Q{i} . Q{j} = {dot}");
                        fail = true;
                    }
                }
            }

            for (int i = 0; i < neigs - 1; i++)
            {
                double dot = 0;

                for (int k = 0; k < nv; k++)
                    dot += (double)d[k + i * nv] * d[k + i * nv];

                if (Math.Abs(dot - 1) > 1e-4)
                {
                    Console.WriteLine($"| Q{i} | = {Math.Sqrt(dot)}");
                    fail = true;
                }              
            }

            Assert.IsFalse(fail);
        }

        [DoNotParallelize]
        [TestMethod]
        [DataRow(false, WarpCoreOptimizationPath.Avx2)]
        [DataRow(false, WarpCoreOptimizationPath.Avx512)]
        [DataRow(true)]
        public void CpdRegDefaultTest(bool useGpu, WarpCoreOptimizationPath opt=WarpCoreOptimizationPath.Maximum)
        {
            SetOptPath(opt);
            Mesh pcl = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            PointCloud pclTarget = DistortPcl(pcl, Vector3.Zero, 1.10f, 0.25f);

            CpdConfiguration cpdCfg = new CpdConfiguration();
            cpdCfg.UseGpu = useGpu;
            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl, cpdCfg);
            Assert.AreEqual(WarpCoreStatus.WCORE_OK, stat);
            Assert.IsNotNull(ctx);
            Console.WriteLine(ctx.ToString());

            WarpCoreStatus regStat = ctx.Register(pclTarget, out PointCloud? pclBent, out CpdResult result);
            RestoreOptPath();
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

            TestUtils.Render(string.Format("CpdRegDefaultTest_{0}_{1}_0.png", useGpu ? "cuda" : "cpu", opt.ToString()),
                new TestRenderItem(TriStyle.PointCloud, pcl, wireCol:Color.Red),
                new TestRenderItem(TriStyle.PointCloud, pclTarget, wireCol:Color.Green),
                new TestRenderItem(TriStyle.PointCloud, pclBent, wireCol:Color.White));
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

            HeadlessRenderer rend = TestUtils.CreateRenderer();
            rend.RasterFormat = new RasterInfo(512, 512);
            Matrix4x4 modelMat = Matrix4x4.CreateTranslation(-0.5f, 0f, -0.5f);
            TestUtils.Render(rend, "GpaTeapotsTest_0.png", modelMat,
               new TestRenderItem(TriStyle.PointCloud, gpa.Mean, wireCol: Color.White),
               new TestRenderItem(TriStyle.PointCloud, gpa.GetTransformed(0), wireCol: Color.DarkRed),
               new TestRenderItem(TriStyle.PointCloud, gpa.GetTransformed(1), wireCol: Color.DarkGreen),
               new TestRenderItem(TriStyle.PointCloud, gpa.GetTransformed(2), wireCol: Color.DarkBlue));
        }

        [TestMethod]
        public void OpaTeapotsTest()
        {
            Mesh pcl1 = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);          
            PointCloud pcl2 = DistortPcl(pcl1, new Vector3(1.0f, 0.0f, 0.0f), 1.10f, 0.1f);

            Rigid3 rigid = RigidTransform.FitOpa(pcl1, pcl2);
            PointCloud? pcl2a = RigidTransform.TransformPosition(pcl2, rigid);
            Assert.IsNotNull(pcl2a);

            HeadlessRenderer rend = TestUtils.CreateRenderer();
            rend.RasterFormat = new RasterInfo(512, 512);
            Matrix4x4 modelMat = Matrix4x4.CreateTranslation(-0.5f, 0f, -0.5f);
            TestUtils.Render(rend, "OpaTeapotsTest_0.png", modelMat,
               new TestRenderItem(TriStyle.PointCloud, pcl1, wireCol: Color.White),
               new TestRenderItem(TriStyle.PointCloud, pcl2, wireCol: Color.FromArgb(40,40,40)),
               new TestRenderItem(TriStyle.PointCloud, pcl2a, wireCol: Color.Red));
        }

        [DoNotParallelize]
        [TestMethod]
        public void ImputeTest()
        {
            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            PointCloud twisted = TranslateTwistPcl(teapot, new Vector3(0.5f, -0.2f, 0.1f), 0.25f);

            int nv = teapot.VertexCount;
            bool[] allow = new bool[nv];

            for(int i = 0; i < nv; i++)
                allow[i] = true;

            for(int i = nv/2; i < nv; i++)
                allow[i] = false;

            PointCloud? imputed = MeshImputation.ImputePositions(teapot, twisted, BitMask.MakeBitMask(allow), 30);
            Assert.IsNotNull(imputed);

            TestUtils.Render("ImputeTest_0.png",
               new TestRenderItem(TriStyle.PointCloud, twisted, wireCol: Color.DarkCyan),
               new TestRenderItem(TriStyle.PointCloud, imputed, wireCol: Color.White));
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
               (i, j) => (hit[j * bitmapSize + i] < 0) ? -1 : t[j * bitmapSize + i]);

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

        [TestMethod]
        public void TrigridNnBarycentricTest()
        {
            (int[] hit, ResultInfoDPtBary[] res, Mesh m) = TrigridNnTestCase(string.Empty, 1, 32, false);
            m.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> pos);
            m.TryGetIndexData(out ReadOnlySpan<FaceIndices> indices);

            int nv = m.VertexCount;
            int nh = hit.Length;
            int numTested = 0;
            for (int i = 0; i < nh; i++)
            {
                if (hit[i] < 0) continue;
                FaceIndices fi = indices[hit[i]];
                Vector3 posStraight = new Vector3(res[i].x, res[i].y, res[i].z);
                Vector3 posFromBary = MiscUtils.SampleTriangleBarycentric(pos, fi, nv, res[i].u, res[i].v);
                float d = Vector3.Distance(posStraight, posFromBary);

                if (d > 1e-6f)
                {
                    Console.WriteLine($"idx = {i}, hit = {hit[i]}, nt = {numTested}");
                    Console.WriteLine("a = " + MiscUtils.FromSoa(pos, fi.I0, nv).ToString());
                    Console.WriteLine("b = " + MiscUtils.FromSoa(pos, fi.I1, nv).ToString());
                    Console.WriteLine("c = " + MiscUtils.FromSoa(pos, fi.I2, nv).ToString());
                    Console.WriteLine($"u = {res[i].u}, v={res[i].v}");
                    Console.WriteLine($"d = {res[i].d}");
                    Console.WriteLine("want: " + posStraight.ToString());
                    Console.WriteLine("got : " + posFromBary.ToString());
                    Console.WriteLine("err = " + d.ToString());
                    Assert.Fail();
                }
                numTested++;
            }
        }

        public (int[], ResultInfoDPtBary[], Mesh) TrigridNnTestCase(string referenceFileName, int gridCells, int bitmapSize, bool render=true)
        {
            Mesh mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            SearchContext.TryInitTrigrid(mesh, gridCells, out SearchContext? ctx);
            Assert.IsNotNull(ctx);

            Aabb bbox = ctx.GetSpan();
            Assert.IsFalse(bbox.IsInvalid);
            Console.WriteLine(bbox.ToString());

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

            if (render)
            {
                Bitmap bmp = TestUtils.RenderAsHeatmap(bitmapSize, bitmapSize, 0, 2,
                    (i, j) => MathF.Sqrt(res[j * bitmapSize + i].d));

                BitmapAsserts.AssertEqual(referenceFileName, bmp);
            }

            ctx.Dispose();

            return (hit, res, mesh);
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

        [TestMethod]
        public void TeapotKMeansTest()
        {
            PointCloud pcl = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            Clustering.FitKMeans(pcl, 100, out int[] labels, out Vector3[] centers);

            int n = pcl.VertexCount;
            for (int i = 0; i < n; i++)
                Assert.IsTrue(labels[i] >= 0 && labels[i] < n);

            MeshBuilder builder = new MeshBuilder();
            List<Vector3> verts = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            verts.AddRange(centers);
            PointCloud pclK = builder.ToPointCloud();

            TestUtils.Render("TeapotKMeansTest_0.png",
              new TestRenderItem(TriStyle.PointCloud, pcl, wireCol:Color.White),
              new TestRenderItem(TriStyle.Landmarks, pclK, col:Color.Red, lmScale:0.05f));
        }

        [TestMethod]
        public void PcaTest()
        {
            TestUtils.LoadBitmapAsFloatGrey("_lena256.png", out float[] bmpData, out int bmpHeight, out int bmpWidth);

            Pca? pca = Pca.Fit(bmpData, bmpWidth, true);
            Assert.IsNotNull(pca);

            Console.WriteLine("Explained variance: " + string.Join(", ", pca.PcVariance.Select((f) => f.ToString("F3"))));

            Bitmap bmpPcs = TestUtils.RenderAsHeatmap(bmpWidth, bmpHeight, 0, 0.2f,
                (c, r) => MathF.Min(0.2f, MathF.Max(0, pca.GetPrincipalComponent(r)[c] + 0.1f)),
                Lut.Create(256, Lut.JetColors));
          

            TestUtils.SaveTestResult("PcaTest_1.png",
                RoundtripPcaTrim(pca, bmpData, bmpHeight, bmpWidth, 1000));

            TestUtils.SaveTestResult("PcaTest_2.png",
               RoundtripPcaTrim(pca, bmpData, bmpHeight, bmpWidth, 10));

            BitmapAsserts.AssertEqual("PcaTest_0.png", bmpPcs);
        }

        private static Bitmap RoundtripPcaTrim(Pca pca, float[] bmpSrc, int height, int width, int numPcsKeep)
        {
            Lut lut = Lut.Create(256, Lut.GreyColors);

            int numScores = pca.NumPcs;
            int dimension = pca.Dimension;

            float[] scores = new float[numScores];
            float[] pred = new float[dimension];
            double rms = 0;

            Bitmap bmp = new Bitmap(width, height);
            unsafe
            {
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);

                for (int i = 0; i < height; i++)
                {
                    ReadOnlySpan<float> orig = bmpSrc.AsSpan().Slice(i * width, width);
                    pca.TryGetScores(orig, scores.AsSpan());
                    
                    for (int j = numPcsKeep; j < numScores; j++)
                        scores[j] = 0;

                    pca.TryPredict(scores, pred);

                    nint ptr = bmpData.Scan0 + i * bmpData.Stride;
                    Span<int> ptrSpan = new Span<int>((void*)ptr, bmpData.Stride);
                    for (int j = 0; j < width; j++)
                    {
                        ptrSpan[j] = lut.Sample(pred[j]).ToArgb();

                        double err = pred[j] - orig[j];
                        rms += err * err;
                    }
                }

                bmp.UnlockBits(bmpData);
            }

            rms = Math.Sqrt(rms / (width * height));
            Console.WriteLine(string.Format("rmse = {0}", rms));

            return bmp;
        }
    }
}
