using System;
using System.IO;
using Warp9.Data;
using Warp9.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Warp9.Test
{
    public static class TestUtils
    {
        public static readonly string AssetsPath = @"../../test/data/";

        public static Stream OpenAsset(string name)
        {
            string path = Path.Combine(AssetsPath, name);

            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public static Mesh LoadObjAsset(string name, ObjImportMode mode)
        {
            using Stream s = TestUtils.OpenAsset(name);
            if (!ObjImport.TryImport(s, mode, out Mesh m, out string errMsg))
                Assert.Inconclusive("Failed to load OBJ asset: " + errMsg);

            return m;
        }
    }
}
