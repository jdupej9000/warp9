using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
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
            Assert.IsFalse(teapot.HasSegment(MeshSegmentType.Normal));

            Mesh teapotNorm = MeshNormals.MakeNormals(teapot);
            Assert.IsTrue(teapotNorm.HasSegment(MeshSegmentType.Normal));

            HeadlessRenderer rend = TestUtils.CreateRenderer(false);
            TestUtils.Render(rend, "WithNormalsTest_0.png", TeapotModelMatrix,
                new TestRenderItem(TriStyle.MeshFilled, teapotNorm, mrs: MeshRenderStyle.PhongBlinn));
        }

        [TestMethod]
        public void VertexSharingTest()
        {
            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            Assert.IsFalse(teapot.HasSegment(MeshSegmentType.Normal));

            Mesh teapotShared = MeshVertexSharing.ShareVerticesByPosition(teapot);
            Assert.AreEqual(3241, teapotShared.VertexCount);
            Assert.AreEqual(teapot.FaceCount, teapotShared.FaceCount);

            Mesh teapotNorm = MeshNormals.MakeNormals(teapotShared);
            Assert.IsTrue(teapotNorm.HasSegment(MeshSegmentType.Normal));

            HeadlessRenderer rend = TestUtils.CreateRenderer(false);
            TestUtils.Render(rend, "VertexSharingTest_0.png", TeapotModelMatrix,
                new TestRenderItem(TriStyle.MeshFilled, teapotNorm, mrs: MeshRenderStyle.PhongBlinn));
        }

        [TestMethod]
        public void MeshFairingTest()
        {
            Mesh teapot = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            Assert.IsFalse(teapot.HasSegment(MeshSegmentType.Normal));

            Mesh faired = MeshFairing.Optimize(teapot, 0.5f).ToMesh();

            HeadlessRenderer rend = TestUtils.CreateRenderer(false);
            TestUtils.Render(rend, "MeshFairingTest_0.png", TeapotModelMatrix,
                new TestRenderItem(TriStyle.MeshFilled, faired, mrs: MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals),
                new TestRenderItem(TriStyle.MeshFilled, teapot, col: Color.Yellow, mrs: MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals));
        }
    }
}
