using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Native;

namespace Warp9.Test
{
    public static class ProcessingTestUtils
    {
        public static void AssertEqual(Rigid3 want, Rigid3 got)
        {
            bool fail = false;

            fail |= (MathF.Abs(want.cs - got.cs) > 1e-4f);
            fail |= (Vector3.Distance(want.offset, got.offset) > 1e-4f);
            fail |= (Vector3.Dot(want.rot0, got.rot0) < 0.99f);
            fail |= (Vector3.Dot(want.rot1, got.rot1) < 0.99f);
            fail |= (Vector3.Dot(want.rot2, got.rot2) < 0.99f);

            if (fail)
            {
                Console.WriteLine("Wanted: " + want.ToString());
                Console.WriteLine("Got   : " + got.ToString());
                Assert.Fail();
            }
        }

        public static void AssertEqual(Vector3 want, Vector3 got)
        {
            if (Vector3.Distance(want, got) > 1e-5f)
            {
                Console.WriteLine("Wanted: " + want.ToString());
                Console.WriteLine("Got   : " + got.ToString());
                Assert.Fail();
            }
        }
    }
}
