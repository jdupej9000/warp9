using Warp9.Viewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Warp9.Test
{
    [TestClass]
    public class ShaderTests
    {
        [TestMethod]
        public void StockShaderCompileTest()
        {
            ShaderRegistry shr = new ShaderRegistry();
            shr.AddShader(StockShaders.VsDefault);
            shr.AddShader(StockShaders.PsDefault);
        }
    }
}