using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Model;
using Warp9.Processing;
using Warp9.Viewer;

namespace Warp9.Test
{
    [TestClass]
    public class ProcessingTests
    {
        public static Matrix4x4 TeapotModelMatrix = Matrix4x4.CreateTranslation(-1.5f, -3.0f, -3.0f);

        [TestMethod]
        public void WithNormalsTest()
        {
            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            Assert.IsFalse(teapot.HasSegment(MeshSegmentSemantic.Normal));

            Mesh teapotNorm = MeshNormals.MakeNormals(teapot);
            Assert.IsTrue(teapotNorm.HasSegment(MeshSegmentSemantic.Normal));

            HeadlessRenderer rend = TestUtils.CreateRenderer(false);
            TestUtils.Render(rend, "WithNormalsTest_0.png", TeapotModelMatrix,
                new TestRenderItem(TriStyle.MeshFilled, teapotNorm, mrs: MeshRenderStyle.PhongBlinn));
        }

        [TestMethod]
        public void VertexSharingTest()
        {
            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            Assert.IsFalse(teapot.HasSegment(MeshSegmentSemantic.Normal));

            Mesh teapotShared = MeshVertexSharing.ShareVerticesByPosition(teapot);
            Assert.AreEqual(3241, teapotShared.VertexCount);
            Assert.AreEqual(teapot.FaceCount, teapotShared.FaceCount);

            Mesh teapotNorm = MeshNormals.MakeNormals(teapotShared);
            Assert.IsTrue(teapotNorm.HasSegment(MeshSegmentSemantic.Normal));

            HeadlessRenderer rend = TestUtils.CreateRenderer(false);
            TestUtils.Render(rend, "VertexSharingTest_0.png", TeapotModelMatrix,
                new TestRenderItem(TriStyle.MeshFilled, teapotNorm, mrs: MeshRenderStyle.PhongBlinn));
        }

        [TestMethod]
        public void MeshFairingTest()
        {
            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            Assert.IsFalse(teapot.HasSegment(MeshSegmentSemantic.Normal));

            Mesh faired = MeshFairing.Optimize(teapot, 0.5f).ToMesh();

            HeadlessRenderer rend = TestUtils.CreateRenderer(false);
            TestUtils.Render(rend, "MeshFairingTest_0.png", TeapotModelMatrix,
                new TestRenderItem(TriStyle.MeshFilled, faired, mrs: MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals),
                new TestRenderItem(TriStyle.MeshFilled, teapot, col: Color.Yellow, mrs: MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals));
        }

        [TestMethod]
        public void ReverseBilateralLandmarkIndicesTest()
        {
            string facesFile = LongRunningTests.GetExternalDependency("faces.w9");

            using Warp9ProjectArchive archive = new Warp9ProjectArchive(facesFile, false);
            using Project project = Project.Load(archive);

            for (int row = 0; row < 10; row++)
            {
                PointCloud pcl = LongRunningTests.GetPointCloudFromProject(project, 21, "Landmarks", row);
                MeshView? view = pcl.GetView(MeshViewKind.Pos3f);
                if (view is null || !view.AsTypedData(out ReadOnlySpan<Vector3> pos))
                {
                    Assert.Fail("Cannot get pcl view.");
                    return; // To make the compiler happy.
                }

                int[] rev = LandmarkUtils.ReverseBilateralLandmarkIndices(pos);
                string order = string.Join(",", rev.Select((t) => t.ToString()));

                Assert.AreEqual("3,2,1,0,4,5,7,6,8", order);
            }
        }
    }
}
