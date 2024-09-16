using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
