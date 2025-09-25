using SharpDX;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Warp9.Data;

namespace Warp9.Native
{
    public static class RigidTransform
    {
        public static PointCloud? TransformPosition(PointCloud pcl, Rigid3 rigid)
        {
            if (!pcl.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> dataPosSrc, out _))
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
                        return PointCloud.FromRawPositions(pcl.VertexCount, dataPosDest);
                    }
                }
            }

            return null;
        }

        public static PclStat3 MakePclStats(PointCloud pcl)
        {
            if (!pcl.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> data, out _))
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
            if (!templ.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> t, out _))
                throw new InvalidOperationException();

            if (!floating.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> x, out _))
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
           
            BufferSegment<Vector3>[] pins = new BufferSegment<Vector3>[n];
            nint[] handles = new nint[n];
            for (int i = 0; i < n; i++)
            {
                pcls[i].TryGetData(MeshSegmentSemantic.Position, out pins[i]);
                handles[i] = pins[i].Lock();
            }

            Rigid3[] xforms = new Rigid3[n];
            byte[] mean = new byte[specimenDataSize];
            GpaResult gpaRes = new GpaResult();
            int nv = specimenDataSize / d / 4;
            WarpCoreStatus ret;

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
           
            result = gpaRes;
            transforms = xforms;
            meanPcl = PointCloud.FromRawPositions(nv, mean);

            for (int i = 0; i < n; i++)
                pins[i].Unlock();

            return ret;
        }
    }
}
