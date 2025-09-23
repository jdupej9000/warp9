using SharpDX;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Warp9.Data;

namespace Warp9.Native
{
    public static class RigidTransform
    {
        public static PointCloud? TransformPosition(PointCloud pcl, Rigid3 rigid)
        {
            if (!pcl.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> dataPosSrc))
                return pcl;

            // TODO: remove copy
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

        public static PclStat3 MakePclStats(PointCloud pcl)
        {
            if (!pcl.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> data))
                throw new InvalidOperationException();

            PclStat3 pclStat = new PclStat3();

            unsafe
            {
                fixed (byte* pdata = &MemoryMarshal.GetReference(data))
                {
                    WarpCore.pcl_stat((nint)pdata, 3, pcl.VertexCount, ref pclStat);
                }
            }

            return pclStat;
        }

        // Find rigid transform floating -> templ.
        public static Rigid3 FitOpa(PointCloud templ, PointCloud floating)
        {
            if (!templ.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> t))
                throw new InvalidOperationException();

            if (!floating.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> x))
                throw new InvalidOperationException();

            Rigid3 ret = new Rigid3();
            WarpCoreStatus s = WarpCoreStatus.WCORE_OK;

            unsafe
            {
                fixed (byte* ptrT = &MemoryMarshal.GetReference(t))
                fixed (byte* ptrX = &MemoryMarshal.GetReference(x))
                {
                    s = (WarpCoreStatus)WarpCore.opa_fit((nint)ptrT, (nint)ptrX, 3, templ.VertexCount, ref ret);
                }
            }

            if (s != WarpCoreStatus.WCORE_OK)
                throw new InvalidOperationException();

            return ret;
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
                pcls[i].TryGetRawDataSegment(MeshSegmentSemantic.Position, -1, out int offset, out int length);
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
