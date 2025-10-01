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
                Assert.AreEqual(false, m.TryGetRawData(mst, out _, out _));
            }
        }

        [TestMethod]
        public void AddBlankChannelTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;

            Mesh m = builder.ToMesh();
            Assert.AreEqual(0, m.VertexCount);
            Assert.AreEqual(0, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> d, out MeshSegmentFormat fmt));
            Assert.AreEqual(0, d.Length);
            Assert.AreEqual(MeshSegmentFormat.Float32x3, fmt);
        }

        [TestMethod]
        public void AddEditChannelTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
            for (int i = 0; i < 6; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            Mesh m = builder.ToMesh();
            Assert.AreEqual(6, m.VertexCount);
            Assert.AreEqual(2, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> d));
            Assert.AreEqual(6, d.Length);
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i, d[i].X);
                Assert.AreEqual(10 * i, d[i].Y);
                Assert.AreEqual(100 * i, d[i].Z);
            }

            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> dr, out MeshSegmentFormat fmt));
            Assert.AreEqual(6 * 12, dr.Length);
            Assert.AreEqual(MeshSegmentFormat.Float32x3, fmt);
        }

        [TestMethod]
        public void AddEditTwoChannelsTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
            for (int i = 0; i < 6; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            List<Vector2> tex = builder.GetSegmentForEditing<Vector2>(MeshSegmentSemantic.Tex0, false).Data;
            for (int i = 0; i < 6; i++)
                tex.Add(new Vector2(i + 100, i + 200));

            Mesh m = builder.ToMesh();
            Assert.AreEqual(6, m.VertexCount);
            Assert.AreEqual(2, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> dpos));
            Assert.AreEqual(true, m.TryGetData(MeshSegmentSemantic.Tex0, out ReadOnlySpan<Vector2> dtex));

            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i, dpos[i].X);
                Assert.AreEqual(10 * i, dpos[i].Y);
                Assert.AreEqual(100 * i, dpos[i].Z);
            }

            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(100 + i, dtex[i].X);
                Assert.AreEqual(200 + i, dtex[i].Y);
            }
        }

        [TestMethod]
        public void AddEditTwoChannelsInvalidTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
            for (int i = 0; i < 6; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            List<Vector2> tex = builder.GetSegmentForEditing<Vector2>(MeshSegmentSemantic.Tex0, false).Data;
            for (int i = 0; i < 3; i++)
                tex.Add(new Vector2(i + 100, i + 200));

            Assert.ThrowsException<InvalidDataException>(() => builder.ToMesh());
           
        }
    }
}
