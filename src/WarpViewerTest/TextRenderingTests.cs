using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Test
{
    [TestClass]
    public class TextRenderingTests
    {
        [TestMethod]
        public void FontDefinitionLoadTest()
        {
            using Stream stream = TestUtils.OpenAsset("segoe-ui-minimal.fnt");
            FontDefinition fnt = FontDefinition.FromStream(stream);

            Assert.AreEqual("segoe-ui-minimal.png", fnt.BitmapFileName);
            Assert.AreEqual("Segoe UI", fnt.FaceName);
            Assert.AreEqual(48, fnt.FontSize);
            Assert.AreEqual(512, fnt.BitmapWidth);
            Assert.AreEqual(512, fnt.BitmapHeight);
            Assert.AreEqual(72, fnt.LineHeight);
        }
    }
}
