using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Native
{
    public static class Clustering
    {
        public static void FitKMeans(PointCloud pcl, int k, out int[] labels, out Vector3[] centers)
        {
            if (!pcl.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> x))
                throw new InvalidOperationException();

            int n = pcl.VertexCount;
            labels = new int[n];
            centers = new Vector3[k];

            float[] ct = new float[3 * k];

            unsafe
            {
                fixed (byte* ptrPcl = &MemoryMarshal.GetReference(x))
                fixed (int* ptrLabels = &MemoryMarshal.GetReference(labels.AsSpan()))
                fixed (float* ptrCenters = &MemoryMarshal.GetReference(ct.AsSpan()))
                {
                    WarpCore.clust_kmeans((nint)ptrPcl, 3, n, k, (nint)ptrCenters, (nint)ptrLabels);
                }
            }

            for(int i = 0; i < k; i++)
                centers[i] = new Vector3(ct[i], ct[i + k], ct[i + 2*k]);
        }
    }
}
