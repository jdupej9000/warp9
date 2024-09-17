using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            foreach (MeshSegmentType mst in Enum.GetValues(typeof(MeshSegmentType)))
            {
                Assert.AreEqual(false, m.TryGetRawData(mst, Mesh.AllCoords, out _));
            }
        }

        [TestMethod]
        public void AddBlankChannelTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);

            Mesh m = builder.ToMesh();
            Assert.AreEqual(0, m.VertexCount);
            Assert.AreEqual(0, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentType.Position, Mesh.AllCoords, out ReadOnlySpan<byte> d));
            Assert.AreEqual(0, d.Length);
        }

        [TestMethod]
        public void AddEditChannelTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            for (int i = 0; i < 5; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            Mesh m = builder.ToMesh();
            Assert.AreEqual(5, m.VertexCount);
            Assert.AreEqual(0, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentType.Position, Mesh.AllCoords, out ReadOnlySpan<byte> d));
            ReadOnlySpan<float> posSoa = MemoryMarshal.Cast<byte, float>(d);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i, posSoa[i]);
                Assert.AreEqual(10 * i, posSoa[i + 5]);
                Assert.AreEqual(100 * i, posSoa[i + 10]);
            }
        }

        [TestMethod]
        public void AddEditTwoChannelsTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            for (int i = 0; i < 5; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            List<Vector2> tex = builder.GetSegmentForEditing<Vector2>(MeshSegmentType.Tex0);
            for (int i = 0; i < 5; i++)
                tex.Add(new Vector2(i + 100, i + 200));

            Mesh m = builder.ToMesh();
            Assert.AreEqual(5, m.VertexCount);
            Assert.AreEqual(0, m.FaceCount);
            Assert.AreEqual(false, m.IsIndexed);

            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentType.Position, Mesh.AllCoords, out ReadOnlySpan<byte> dpos));
            Assert.AreEqual(true, m.TryGetRawData(MeshSegmentType.Tex0, Mesh.AllCoords, out ReadOnlySpan<byte> dtex));

            ReadOnlySpan<float> posSoa = MemoryMarshal.Cast<byte, float>(dpos);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i, posSoa[i]);
                Assert.AreEqual(10 * i, posSoa[i + 5]);
                Assert.AreEqual(100 * i, posSoa[i + 10]);
            }

            ReadOnlySpan<float> texSoa = MemoryMarshal.Cast<byte, float>(dtex);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i + 100, texSoa[i]);
                Assert.AreEqual(i + 200, texSoa[i + 5]);
            }
        }

        [TestMethod]
        public void AddEditTwoChannelsInvalidTest()
        {
            MeshBuilder builder = new MeshBuilder();
            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            for (int i = 0; i < 5; i++)
                pos.Add(new Vector3(i, 10 * i, 100 * i));

            List<Vector2> tex = builder.GetSegmentForEditing<Vector2>(MeshSegmentType.Tex0);
            for (int i = 0; i < 3; i++)
                tex.Add(new Vector2(i + 100, i + 200));

            Assert.ThrowsException<InvalidDataException>(() => builder.ToMesh());
           
        }
    }
}
