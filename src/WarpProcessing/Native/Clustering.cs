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
                    WarpCore.clust_fit((nint)ptrPcl, 3, n, k, (nint)ptrCenters, (nint)ptrLabels, (int)WCORE_CLUST_METHOD.KMEANS);
                }
            }
        }

        public static void FitGridSel(PointCloud pcl, int grid_dim, bool central, out Vector3[] centers)
        {
            if (!pcl.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> x, out MeshSegmentFormat fmt) ||
                fmt != MeshSegmentFormat.Float32x3)
                throw new InvalidOperationException();

            int n = pcl.VertexCount;
            int kmax = grid_dim * grid_dim * grid_dim;
            Vector3[] centersBuff = new Vector3[kmax];
            int kactual = 0;

            unsafe
            {
                fixed (byte* ptrPcl = &MemoryMarshal.GetReference(x))
                fixed (Vector3* ptrCenters = &MemoryMarshal.GetReference(centersBuff.AsSpan()))
                {
                    kactual = WarpCore.clust_fit((nint)ptrPcl, 3, n, grid_dim, (nint)ptrCenters, nint.Zero, 
                        central ? (int)WCORE_CLUST_METHOD.GRIDSEL_CENTRAL : (int)WCORE_CLUST_METHOD.GRIDSEL);
                }
            }

            if (kactual < 0)
            {
                centers = Array.Empty<Vector3>();
            }

            centers = centersBuff.Take(kactual).ToArray();
        }
    }
}
