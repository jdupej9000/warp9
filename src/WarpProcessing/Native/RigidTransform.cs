using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Native
{
    public static class RigidTransform
    {
        public static PointCloud? TransformPosition(PointCloud pcl, Rigid3 rigid)
        {
            if (!pcl.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> dataPosSrc))
                return pcl;

            byte[] dataPosDest = new byte[dataPosSrc.Length];
            Span<Rigid3> rigidSpan = stackalloc Rigid3[1];
            rigidSpan[0] = rigid;

            unsafe
            {
                fixed (byte* dataPosSrcPtr = &MemoryMarshal.GetReference(dataPosSrc))
                fixed (byte* dataPosDestPtr = &MemoryMarshal.GetReference(dataPosDest.AsSpan()))
                fixed (byte* xformPtr = &MemoryMarshal.GetReference(MemoryMarshal.Cast<Rigid3, byte>(rigidSpan)))
                {
                    WarpCoreStatus ret = (WarpCoreStatus)WarpCore.rigid_transform(
                        (nint)dataPosSrcPtr, 3, pcl.VertexCount, (nint)xformPtr, (nint)dataPosDestPtr);

                    if (ret == WarpCoreStatus.WCORE_OK)
                    {
                        return PointCloud.FromRawSoaPositions(pcl.VertexCount, dataPosDest);
                    }
                }
            }

            return null;
        }
    }
}
