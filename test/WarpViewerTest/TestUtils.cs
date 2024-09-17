using System;
using System.IO;

namespace Warp9.Test
{
    internal static class TestUtils
    {
        public static readonly string AssetsPath = @"../../test/data/";

        public static Stream OpenAsset(string name)
        {
            string path = Path.Combine(AssetsPath, name);

            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }
    }
}
