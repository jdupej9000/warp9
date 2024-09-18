﻿using System;
using Warp9.Data;
using Warp9.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

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
    }
}
