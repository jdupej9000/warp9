using Microsoft.VisualStudio.TestTools.UnitTesting;
using Warp9.Viewer;

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
            shr.AddShader(StockShaders.VsDefaultInstanced);
            shr.AddShader(StockShaders.PsDefault);
            shr.AddShader(StockShaders.VsText);
            shr.AddShader(StockShaders.PsText);
        }
    }
}