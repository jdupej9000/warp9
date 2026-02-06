using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.IO;
using Warp9.Utils;

namespace Warp9.Test
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        [DataRow(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 4)]
        [DataRow(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4)]
        [DataRow(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
        public void FrobeniusNormTest(float m00, float m01, float m02, float m03, 
            float m10, float m11, float m12, float m13, 
            float m20, float m21, float m22, float m23, 
            float m30, float m31, float m32, float m33, 
            float norm)
        {
            Matrix4x4 m = new Matrix4x4(m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33);
            float retNorm = MiscUtils.FrobeniusNorm(m);
            Assert.IsTrue(MathF.Abs(retNorm - norm) < 1e-4f);
        }

        [TestMethod]
        [DataRow("one two", 3, ' ', 4)]
        [DataRow("   indent", 0, ' ', 3)]
        [DataRow("noindent", 0, ' ', 0)]
        public void SkipCharTest(string s, int pos, char skip, int ret)
        {
            Assert.AreEqual(ret, IO.IoUtils.Skip(s, pos, skip));
        }

        [TestMethod]
        [DataRow("one two", 0, ' ', 3)]
        public void SkipAllButTest(string s, int pos, char skip, int ret)
        {
            Assert.AreEqual(ret, IO.IoUtils.SkipAllBut(s, pos, skip));
        }

        [TestMethod]
        [DataRow("noint", 2, 2)]
        [DataRow("99luftballons", 0, 2)]
        public void SkipIntTest(string s, int pos, int ret)
        {
            Assert.AreEqual(ret, IO.IoUtils.SkipInt(s, pos));
        }

        
        [TestMethod]
        [DataRow("", 0, 0)]
        [DataRow("noint", 2, 5)]
        [DataRow("99luftballons", 0, 0)]
        [DataRow("99luftballons", 2, 13)]
        public void SkipNonIntTest(string s, int pos, int ret)
        {
            Assert.AreEqual(ret, IO.IoUtils.SkipNonInt(s, pos));
        }

        [TestMethod]
        [DataRow("", 0, ';', new float[0] { })]
        [DataRow("3.14", 0, ';', new float[1] { 3.14f })]
        [DataRow("1.0;2.8;3.4", 0, ';', new float[3] { 1.0f, 2.8f, 3.4f })]
        public void ParseSeparatedFloatsTest(string s, int pos, char sep, float[] want)
        {
            float[] got = new float[10];
            int numItems = IoUtils.ParseSeparatedFloats(s, pos, sep, got.AsSpan());
            Assert.AreEqual(numItems, want.Length);

            for(int i = 0; i < numItems; i++)
                Assert.AreEqual(want[i], got[i]);
        }

        [TestMethod]
        [DataRow(0f, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
        [DataRow(0.5f, 0, 0, 0, 1, 0, 0, 0, 1, 0)]
        [DataRow(0.5f, 0, 0, 0, 1, 0, 0, 0, 0, 1)]
        [DataRow(0.5f, 0, 0, 0, 0, 1, 0, 0, 0, 1)]
        [DataRow(2f, 0, 0, 0, 2, 0, 0, 0, 2, 0)]
        public void TriangleAreaTest(float expected, float x0, float y0, float z0, float x1, float y1, float z1, float x2, float y2, float z2)
        {
            float area = MeshUtils.TriangleAreaHeron(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), new Vector3(x2, y2, z2));
            if (MathF.Abs(area - expected) > 1e-6f)
            {
                Console.WriteLine($"Wanted {expected}, got {area}.");
                Assert.Fail();
            }
        }
    }
}
