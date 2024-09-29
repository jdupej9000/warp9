using System;
using Warp9.Data;
using Warp9.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.Generic;
using System.Numerics;

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
