using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using Warp9.Data;

namespace Warp9.Test
{
    [TestClass]
    public class LutTests
    {
        private static void LutSampleAtStopCase(int width, (float, Color)[] stops)
        {
            Lut lut = Lut.Create(width, stops);
            foreach(var s in stops)
                Assert.AreEqual(s.Item2, lut.Sample(s.Item1));
        }


        [TestMethod]
        public void LutCreateTest()
        {
            Lut lut = Lut.Create(256, Lut.FastColors);
            Assert.AreEqual(256, lut.NumPixels);
        }

        [TestMethod]
        public void LutSampleAtStopTest()
        {
            LutSampleAtStopCase(128, Lut.FastColors);
            LutSampleAtStopCase(256, Lut.FastColors);
            LutSampleAtStopCase(1024, Lut.FastColors);
            LutSampleAtStopCase(256, Lut.SmoothCoolWarmColors);
            LutSampleAtStopCase(512, Lut.ViridisColors);
        }
    }
}