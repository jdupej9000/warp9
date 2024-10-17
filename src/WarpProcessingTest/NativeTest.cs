using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Native;

namespace Warp9.Test
{
    [TestClass]
    public class NativeTest
    {
        [TestMethod]
        public void CpdInitDefaultTest()
        {
            PointCloud pcl = TestUtils.LoadObjAsset("teapot.obj", IO.ObjImportMode.PositionsOnly);
            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl);

            Assert.AreEqual(WarpCoreStatus.WCORE_OK, stat);
            Assert.IsNotNull(ctx);

            Console.WriteLine(ctx.ToString());
        }
    }
}
