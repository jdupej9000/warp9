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
using Warp9.Utils;
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
            string facesFile = ProcessingTestUtils.GetExternalDependency("faces.w9");

            using Warp9ProjectArchive archive = new Warp9ProjectArchive(facesFile, false);
            using Project project = Project.Load(archive);

            for (int row = 0; row < 10; row++)
            {
                PointCloud pcl = ProcessingTestUtils.GetPointCloudFromProject(project, 21, "Landmarks", row);
                if (!(pcl.TryGetData( MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> pos)))
                {
                    Assert.Fail("Cannot get pcl view.");
                    return; // To make the compiler happy.
                }

                int[] rev = LandmarkUtils.ReverseBilateralLandmarkIndices(pos);
                string order = string.Join(",", rev.Select((t) => t.ToString()));

                Assert.AreEqual("3,2,1,0,4,5,7,6,8,10,9,13,14,11,12,15,16,17,19,18", order);
            }
        }

        [TestMethod]
        public void SymmetrizeMeshRigidTest()
        {
            string facesFile = ProcessingTestUtils.GetExternalDependency("faces.w9");

            using Warp9ProjectArchive archive = new Warp9ProjectArchive(facesFile, false);
            using Project project = Project.Load(archive);
            Mesh m = ProcessingTestUtils.GetMeshFromProject(project, 21, "Model", 0);
            PointCloud l = ProcessingTestUtils.GetPointCloudFromProject(project, 21, "Landmarks", 0);

            PointCloud symm = MeshSymmetrize.MakeSymmetricRigid(m, l);

            HeadlessRenderer rend = TestUtils.CreateRenderer(false);
            TestUtils.Render(rend, "SymmetrizeMeshRigidTest_0.png", Matrix4x4.CreateScale(0.025f),
                new TestRenderItem(TriStyle.MeshFilled, Mesh.FromPointCloud(symm, m), col: Color.Gray, mrs: MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals),
                new TestRenderItem(TriStyle.MeshWire, m, mrs: MeshRenderStyle.ColorFlat, wireCol: Color.Blue));
        }

        [TestMethod]
        [DataRow(new bool[4] { true, false, true, false }, 1, new int[1] { 0b0101 })]
        [DataRow(new bool[4] { true, false, true, false }, 2, new int[1] { 0b00110011 })]
        [DataRow(new bool[4] { true, false, true, false }, 3, new int[1] { 0b000111000111 })]
        public void MakeBitMaskTest(bool[] mask, int rep, int[] bin)
        {
            int[] binRes = BitMask.MakeBitMask(mask.AsSpan(), rep);
            Assert.AreEqual(bin.Length, binRes.Length);

            int numErr = 0;
            for (int i = 0; i < binRes.Length; i++)
            {
                if (bin[i] != binRes[i]) numErr++;
            }

            Assert.AreEqual(0, numErr);
        }
    }
}
