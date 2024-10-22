using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Warp9.Data;
using Warp9.Native;

namespace Warp9.Test
{
    [TestClass]
    public class NativeTest
    {
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
            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl);

            Assert.AreEqual(WarpCoreStatus.WCORE_OK, stat);
            Assert.IsNotNull(ctx);

            Console.WriteLine(ctx.ToString());
        }

        [TestMethod]
        public void CpdRegDefaultTest()
        {
            Mesh pcl = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            //PointCloud pclTarget = DistortPcl(pcl, new Vector3(0.5f, 0.2f, -0.1f), 1.10f, 0.25f);
            PointCloud pclTarget = DistortPcl(pcl, Vector3.Zero, 1.10f, 0.25f);

            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl, w:0.1f);
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
            PointCloud pcl2 = DistortPcl(pcl1, new Vector3(0.5f, 0.2f, -0.1f), 0.80f, 0.05f);
            PointCloud pcl3 = DistortPcl(pcl1, Vector3.Zero, 1.10f, 0.05f);

            PointCloud[] pcls = new PointCloud[3] { pcl1, pcl2, pcl3 };

            WarpCoreStatus s = (WarpCoreStatus)RigidTransform.FitGpa(pcls, out PointCloud mean, out Rigid3[] xforms, out GpaResult res);
            Console.WriteLine(res.ToString());

            Assert.AreEqual(3, xforms.Length);
            Assert.AreEqual(pcl1.VertexCount, mean.VertexCount);
            Assert.AreEqual(WarpCoreStatus.WCORE_OK, s);
        }

        [TestMethod]
        public void TrigridRaycastTest()
        {
            Mesh mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            SearchContext.TryInitSearch(mesh, SEARCH_STRUCTURE.SEARCH_TRIGRID3, out SearchContext? ctx);
            Assert.IsNotNull(ctx);

            const int Size = 128;
            TestUtils.GenerateRays(Size, Size, out Vector3[] p0, out Vector3[] d);
            int n = Size * Size;
            int[] hit = new int[n];
            float[] t = new float[n];

            for (int i = 0; i < n; i++)
                p0[i] -= new Vector3(-1.5f, -3.0f, -3.0f);

            DateTime t0 = DateTime.Now;
            ctx.RaycastAos(p0.AsSpan(), d.AsSpan(), n, hit.AsSpan(), t.AsSpan());
            DateTime t1 = DateTime.Now;
            double seconds = (t1 - t0).TotalSeconds;

            Console.WriteLine(string.Format("{0} rays in {1:F3} seconds, {2:F1} rays per second",
                Size * Size, seconds, Size * Size / seconds));

            Lut lut = Lut.Create(256, Lut.FastColors);

            Bitmap bmp = new Bitmap(Size, Size);
            float range0 = 0; //t.Min();
            float range1 = 3;// t.Max();
            for (int j = 0; j < Size; j++)
            {
                for (int i = 0; i < Size; i++)
                {
                    float r = (t[j * Size + i] - range0) / (range1 - range0);
                    bmp.SetPixel(i, j, lut.Sample(r));
                    //if(hit[i] >= 0)
                     //   bmp.SetPixel(i, j, Color.FromArgb(hit[i] % 256, 0, 0));
                }
            }

            BitmapAsserts.AssertEqual("TrigridRaycastTest_0.png", bmp);

            ctx.Dispose();
        }

        [TestMethod]
        public void PclStatsTest()
        {
            Mesh mesh = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            PclStat3 stat = RigidTransform.MakePclStats(mesh);

            Console.WriteLine(string.Format("x0={0}, x1={1}, center={2}, cs={3}",
                stat.x0.ToString(), stat.x1.ToString(), stat.center.ToString(), stat.size));
        }
    }
}
