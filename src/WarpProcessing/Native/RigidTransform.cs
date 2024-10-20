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

        public static WarpCoreStatus FitGpa(IReadOnlyList<PointCloud> pcls, out PointCloud meanPcl, out Rigid3[] transforms, out GpaResult result)
        {
            const int d = 3;
            int n = pcls.Count;
            int specimenDataSize = -1;
            int numOk = 0;

            GCHandle[] pins = new GCHandle[n];
            nint[] handles = new nint[n];
            for (int i = 0; i < n; i++)
            { 
                pcls[i].TryGetRawDataSegment(MeshSegmentType.Position, -1, out int offset, out int length);
                if (specimenDataSize == -1)
                {
                    specimenDataSize = length;
                }
                else if (specimenDataSize != length)
                {
                    break;
                }

                numOk = i + 1;
                pins[i] = GCHandle.Alloc(pcls[i].RawData, GCHandleType.Pinned);
                handles[i] = pins[i].AddrOfPinnedObject() + offset;
            }

            Rigid3[] xforms = new Rigid3[n];
            byte[] mean = new byte[specimenDataSize];
            GpaResult gpaRes = new GpaResult();
            int nv = specimenDataSize / d / 4;
            WarpCoreStatus ret;

            if (numOk == n)
            {
                unsafe
                {
                    fixed (nint* ppdata = &MemoryMarshal.GetReference(handles.AsSpan()))
                    fixed (Rigid3* pxforms = &MemoryMarshal.GetReference(xforms.AsSpan()))
                    fixed (byte* pmean = &MemoryMarshal.GetReference(mean.AsSpan()))
                    {
                        ret = (WarpCoreStatus)WarpCore.gpa_fit(
                            (nint)ppdata, d, n, specimenDataSize / 4 / d, (nint)pxforms, (nint)pmean, ref gpaRes);
                    }
                }
            }
            else
            {
                ret = WarpCoreStatus.WCORE_INVALID_ARGUMENT;
            }

            result = gpaRes;
            transforms = xforms;
            meanPcl = PointCloud.FromRawSoaPositions(nv, mean);

            for (int i = 0; i < numOk; i++)
                pins[i].Free();

            return ret;
        }
    }
}
