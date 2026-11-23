using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Native;
using Warp9.Utils;

namespace Warp9.Test
{
    [TestClass]
    public class MathTest
    {
        [TestMethod]
        public void RigidIdentityTest()
        {
            ProcessingTestUtils.AssertEqual(
                Rigid3.Identity,
                Rigid3.Translation(Vector3.Zero));

            ProcessingTestUtils.AssertEqual(
                Rigid3.Identity,
                Rigid3.Scale(1));

            ProcessingTestUtils.AssertEqual(
                Rigid3.Identity,
                Rigid3.RotateAboutZ(0));
        }

        [TestMethod]
        public void RigidCombineTest()
        {
            ProcessingTestUtils.AssertEqual(
                Rigid3.Identity,
                Rigid3.Identity * Rigid3.Identity);

            ProcessingTestUtils.AssertEqual(
                Rigid3.Scale(6),
                Rigid3.Scale(2) * Rigid3.Scale(3));

            ProcessingTestUtils.AssertEqual(
                Rigid3.Translation(new System.Numerics.Vector3(1, 2, 0)),
                Rigid3.Translation(new System.Numerics.Vector3(1, 0, 0)) * Rigid3.Translation(new System.Numerics.Vector3(0, 2, 0)));

            ProcessingTestUtils.AssertEqual(
                Rigid3.RotateAboutZ(3),
                Rigid3.RotateAboutZ(1) * Rigid3.RotateAboutZ(2));
        }

        [TestMethod]
        public void InvertTest()
        {
            ProcessingTestUtils.AssertEqual(
                Rigid3.Identity,
                Rigid3.Identity.Invert());

            ProcessingTestUtils.AssertEqual(
                Rigid3.Scale(0.5f),
                Rigid3.Scale(2f).Invert());

            ProcessingTestUtils.AssertEqual(
                Rigid3.RotateAboutZ(0.5f),
                Rigid3.RotateAboutZ(-0.5f).Invert());

            ProcessingTestUtils.AssertEqual(
                Rigid3.Translation(new Vector3(1, 2, 3)),
                Rigid3.Translation(new Vector3(-1, -2, -3)).Invert());
        }

        [TestMethod]
        public void RigidComplexTest()
        {
            ProcessingTestUtils.AssertEqual(
                Rigid3.Scale(2) * Rigid3.RotateAboutZ(1),
                (Rigid3.Scale(0.5f) * Rigid3.RotateAboutZ(-1)).Invert());          
        }

        [TestMethod]
        public void RigidTransformTest()
        {
            ProcessingTestUtils.AssertEqual(
                new Vector3(1, 2, 3),
                Rigid3.Identity.Transform(new Vector3(1, 2, 3)));

            ProcessingTestUtils.AssertEqual(
                new Vector3(10, 20, 30),
                Rigid3.Scale(10f).Transform(new Vector3(1, 2, 3)));

            ProcessingTestUtils.AssertEqual(
                new Vector3(11, 22, 33),
                Rigid3.Translation(new Vector3(-10,-20,-30)).Transform(new Vector3(1, 2, 3)));

            ProcessingTestUtils.AssertEqual(
                new Vector3(-1, -2, 3),
                Rigid3.RotateAboutZ(MathF.PI).Transform(new Vector3(1, 2, 3)));

            ProcessingTestUtils.AssertEqual(
               new Vector3(2, -1, 3),
               Rigid3.RotateAboutZ(MathF.PI / 2).Transform(new Vector3(1, 2, 3)));
        }

        [TestMethod]
        public void RigidTransformConsistencyTest()
        {
            Rigid3 rigid = Rigid3.Translation(new Vector3(-1, 0, 1)) *
                Rigid3.Scale(0.4f) *
                Rigid3.RotateAboutZ(0.1f);

            Console.WriteLine(rigid.ToString());

            PointCloud pcl1 = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            PointCloud pcl2 = RigidTransform.TransformPosition(pcl1, rigid)!;

            Assert.IsTrue(
                pcl1.TryGetData(MeshSegmentSemantic.Position, out BufferSegment<Vector3> pos1) &
                pcl2.TryGetData(MeshSegmentSemantic.Position, out BufferSegment<Vector3> pos2));

            for (int i = 0; i < pos1.Count; i++)
                ProcessingTestUtils.AssertEqual(rigid.Transform(pos1.Data[i]), pos2.Data[i]);
        }

        [TestMethod]
        [DataRow(-0.1, 2.368961)]
        [DataRow(1, 0)]
        [DataRow(2, 0)]
        [DataRow(3, 0.6931472)]
        [DataRow(4, 1.791759)]
        public void LogGammaTest(double x, double expected)
        {
            double lg = MathUtils.LogGamma(x);
            Assert.IsTrue(Math.Abs(expected - lg) < 1e-6,
                 $"Wanted {expected}, got {lg}.");
        }

        [TestMethod]        
        public void WelchTestTest()
        {
            float[] a = [-0.01112665f, -0.22385566f, 0.41129160f, -0.52425784f, 1.55907586f, 2.95639539f, 0.19795464f, -1.81770239f, 0.36817302f, 1.41356318f];
            float[] b = [-0.068592729f, -0.393753915f, -0.600497711f, -0.142202350f, -0.997982974f, 0.024891781f, -0.238450122f, -0.001190203f, -0.500847654f, 0.384637404f];
            double expected_pval = 0.1396;

            double pval = MathUtils.WelchTest(a, b);
            Assert.IsTrue(Math.Abs(expected_pval - pval) < 1e-4,
                $"Wanted {expected_pval}, got {pval}.");
        }
    }
}
