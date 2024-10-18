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
            PointCloud pclTarget = DistortPcl(pcl, new Vector3(0.5f, 0.2f, -0.1f), 1.10f, 0.25f);

            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl, w:0.1f);
            Assert.AreEqual(WarpCoreStatus.WCORE_OK, stat);
            Assert.IsNotNull(ctx);

            WarpCoreStatus regStat = ctx.Register(pclTarget, out PointCloud? pclBent, out CpdResult result);
            Console.WriteLine(result.ToString());
            Assert.IsNotNull(pclBent);
            Assert.AreEqual(regStat, WarpCoreStatus.WCORE_OK);
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
    }
}
