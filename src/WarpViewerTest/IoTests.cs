using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Warp9.Data;
using Warp9.IO;

namespace Warp9.Test
{
    [TestClass]
    public class IoTests
    {
        [TestMethod]
        public void ImportTeapotObjTest()
        {
            using Stream s = TestUtils.OpenAsset("teapot.obj");
            if (!ObjImport.TryImport(s, ObjImportMode.PositionsOnly, out Mesh m, out string errMsg))
            {
                Console.WriteLine(errMsg);
                Assert.Fail();
            }

            Assert.IsTrue(m.IsIndexed);
            Assert.AreEqual(6320, m.FaceCount);
            Assert.AreEqual(3644, m.VertexCount);
        }

        [TestMethod]
        public void ImportSuzanneObjTest()
        {
            using Stream s = TestUtils.OpenAsset("suzanne.obj");
            if (!ObjImport.TryImport(s, ObjImportMode.AllUnshared, out Mesh m, out string errMsg))
            {
                Console.WriteLine(errMsg);
                Assert.Fail();
            }

            Assert.IsFalse(m.IsIndexed);
            Assert.AreEqual(15488, m.FaceCount);
            Assert.AreEqual(15488 * 3, m.VertexCount);
        }

        [TestMethod]
        public void LoadExt()
        {
            using FileStream fs = new FileStream("D:\\ref-1.w9mesh", FileMode.Open, FileAccess.Read);
            Assert.IsTrue(WarpBinImport.TryImport(fs, out Mesh? m));
            Assert.IsNotNull(m);
        }

        [TestMethod]
        public void TeapotW9MeshRoundtripTest()
        {
            Mesh m0 = TestUtils.LoadObjAsset("teapot.obj", ObjImportMode.PositionsOnly);
            Assert.IsTrue(m0.IsIndexed);
            Assert.AreEqual(6320, m0.FaceCount);
            Assert.AreEqual(3644, m0.VertexCount);

            using MemoryStream ms1 = new MemoryStream();
            WarpBinExport.ExportMesh(ms1, m0, null);
            ms1.Seek(0, SeekOrigin.Begin);
            Assert.IsTrue(ms1.Length > 0);

            Assert.IsTrue(WarpBinImport.TryImport(ms1, out Mesh? m2));
            Assert.IsNotNull(m2);
            Assert.AreEqual(6320, m2.FaceCount);
            Assert.AreEqual(3644, m2.VertexCount);

            int nt = m2.FaceCount;
            Assert.IsTrue(m0.TryGetIndexData(out ReadOnlySpan<FaceIndices> idx0));
            Assert.IsTrue(m2.TryGetIndexData(out ReadOnlySpan<FaceIndices> idx2));
            for (int i = 0; i < nt; i++)
                Assert.AreEqual(idx0[i], idx2[i]);

            MeshView? v0 = m0.GetView(MeshViewKind.Pos3f);
            Assert.IsNotNull(v0);
            Assert.IsTrue(v0.AsTypedData(out ReadOnlySpan<Vector3> vx0));

            MeshView? v2 = m2.GetView(MeshViewKind.Pos3f);
            Assert.IsNotNull(v2);
            Assert.IsTrue(v2.AsTypedData(out ReadOnlySpan<Vector3> vx2));

            int nv = m2.VertexCount;
            for (int i = 0; i < nv; i++)
                Assert.IsTrue(Vector3.Distance(vx0[i], vx2[i]) < 1e-6f);
        }

        [TestMethod]
        public void EmptyPointCloudWarpBinExportTest()
        {
            using MemoryStream stream = new MemoryStream();
            WarpBinExport.ExportPcl(stream, PointCloud.Empty);

            Assert.AreEqual(8, stream.Length); // just the header
        }

        [TestMethod]
        public void SimplePointCloudWarpBinTransportTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            for (int i = 0; i < 5; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            PointCloud pcl = builder.ToPointCloud();

            WarpBinExportSettings settings = new WarpBinExportSettings()
            { 
                PositionFormat = ChunkEncoding.Float32 
            };

            using MemoryStream stream = new MemoryStream();
            WarpBinExport.ExportPcl(stream, pcl, settings);

            stream.Seek(0, SeekOrigin.Begin);

            Assert.IsTrue(WarpBinImport.TryImport(stream, out Mesh? pclImp));
            MeshAsserts.AssertPclEqual(pcl, pclImp);
        }

        [TestMethod]
        public void SimplePointCloudWarpBinFixed16TransportTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            for (int i = 0; i < 5; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            PointCloud pcl = builder.ToPointCloud();

            WarpBinExportSettings settings = new WarpBinExportSettings()
            {
                PositionFormat = ChunkEncoding.Fixed16
            };

            using MemoryStream stream = new MemoryStream();
            WarpBinExport.ExportPcl(stream, pcl, settings);

            stream.Seek(0, SeekOrigin.Begin);

            Assert.IsTrue(WarpBinImport.TryImport(stream, out Mesh? pclImp));
            MeshAsserts.AssertPclEqual(pcl, pclImp);
        }
    }
}
