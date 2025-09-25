using SharpDX;
using System;
using System.Buffers;
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
            if (!pcl.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> x, out MeshSegmentFormat fmt) ||
                fmt != MeshSegmentFormat.Float32x3)
                throw new InvalidOperationException();

            int n = pcl.VertexCount;
            labels = new int[n];
            centers = new Vector3[k];

            unsafe
            {
                fixed (byte* ptrPcl = &MemoryMarshal.GetReference(x))
                fixed (int* ptrLabels = &MemoryMarshal.GetReference(labels.AsSpan()))
                fixed (Vector3* ptrCenters = &MemoryMarshal.GetReference(centers.AsSpan()))
                {
                    WarpCore.clust_kmeans((nint)ptrPcl, 3, n, k, (nint)ptrCenters, (nint)ptrLabels);
                }
            }
        }
    }
}
