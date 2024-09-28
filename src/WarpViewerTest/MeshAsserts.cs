using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Test
{
    public static class MeshAsserts
    {
        private static int NumErrors(ReadOnlySpan<float> arr1, ReadOnlySpan<float> arr2, float tol)
        {
            int numErr = 0;
            for (int i = 0; i < arr1.Length; i++)
            {
                if (MathF.Abs(arr1[i] - arr2[i]) > tol) 
                    numErr++;
            }

            return numErr;
        }

        public static void AssertPclEqual(PointCloud pcl1, PointCloud pcl2, float tol = 1e-6f)
        {
            Assert.AreEqual(pcl1.VertexCount, pcl2.VertexCount);

            foreach (MeshSegmentType mst in Enum.GetValues(typeof(MeshSegmentType)))
            {
                bool gotSeg1 = pcl1.TryGetRawData(mst, -1, out ReadOnlySpan<byte> data1);
                bool gotSeg2 = pcl1.TryGetRawData(mst, -1, out ReadOnlySpan<byte> data2);

                Assert.AreEqual(gotSeg1, gotSeg2);

                // TODO: switch on segment type
                if (gotSeg1)
                {
                    Assert.AreEqual(data1.Length, data2.Length);
                    Assert.AreEqual(0, NumErrors(MemoryMarshal.Cast<byte, float>(data1), MemoryMarshal.Cast<byte, float>(data2), tol));
                }
            }
        }
    }
}
