using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Warp9.Data;

namespace Warp9.Test
{
    [TestClass]
    public class MeshTest
    {
        [TestMethod]
        public void EmptyMeshTest()
        {
            MeshBuilder builder = new MeshBuilder();
            Mesh m = builder.ToMesh();

            Assert.AreEqual(0, m.VertexCount);
            Assert.AreEqual(0, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            foreach (MeshSegmentSemantic mst in Enum.GetValues(typeof(MeshSegmentSemantic)))
            {
                Assert.AreEqual(false, m.TryGetRawData(mst, Mesh.AllCoords, out _));
            }
        }

        [TestMethod]
        public void AddBlankChannelTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);

            Mesh m = builder.ToMesh();
            Assert.AreEqual(0, m.VertexCount);
            Assert.AreEqual(0, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentSemantic.Position, Mesh.AllCoords, out ReadOnlySpan<byte> d));
            Assert.AreEqual(0, d.Length);
        }

        [TestMethod]
        public void AddEditChannelTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);
            for (int i = 0; i < 6; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            Mesh m = builder.ToMesh();
            Assert.AreEqual(6, m.VertexCount);
            Assert.AreEqual(2, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentSemantic.Position, Mesh.AllCoords, out ReadOnlySpan<byte> d));
            ReadOnlySpan<float> posSoa = MemoryMarshal.Cast<byte, float>(d);
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i, posSoa[i]);
                Assert.AreEqual(10 * i, posSoa[i + 6]);
                Assert.AreEqual(100 * i, posSoa[i + 12]);
            }
        }

        [TestMethod]
        public void AddEditTwoChannelsTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);
            for (int i = 0; i < 6; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            List<Vector2> tex = builder.GetSegmentForEditing<Vector2>(MeshSegmentSemantic.Tex0);
            for (int i = 0; i < 6; i++)
                tex.Add(new Vector2(i + 100, i + 200));

            Mesh m = builder.ToMesh();
            Assert.AreEqual(6, m.VertexCount);
            Assert.AreEqual(2, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentSemantic.Position, Mesh.AllCoords, out ReadOnlySpan<byte> dpos));
            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentSemantic.Tex0, Mesh.AllCoords, out ReadOnlySpan<byte> dtex));

            ReadOnlySpan<float> posSoa = MemoryMarshal.Cast<byte, float>(dpos);
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i, posSoa[i]);
                Assert.AreEqual(10 * i, posSoa[i + 6]);
                Assert.AreEqual(100 * i, posSoa[i + 12]);
            }

            ReadOnlySpan<float> texSoa = MemoryMarshal.Cast<byte, float>(dtex);
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i + 100, texSoa[i]);
                Assert.AreEqual(i + 200, texSoa[i + 6]);
            }
        }

        [TestMethod]
        public void AddEditTwoChannelsInvalidTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);
            for (int i = 0; i < 6; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            List<Vector2> tex = builder.GetSegmentForEditing<Vector2>(MeshSegmentSemantic.Tex0);
            for (int i = 0; i < 3; i++)
                tex.Add(new Vector2(i + 100, i + 200));

            Assert.ThrowsException<InvalidDataException>(() => builder.ToMesh());
           
        }
    }
}
