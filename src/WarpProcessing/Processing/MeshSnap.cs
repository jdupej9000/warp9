using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Native;

namespace Warp9.Processing
{
    public static class MeshSnap
    {
        public static PointCloud? ProjectToNearest(PointCloud src, Mesh target, int gridSize = 16)
        {
            if (WarpCoreStatus.WCORE_OK != SearchContext.TryInitTrigrid(target, gridSize, out SearchContext? searchCtx) ||
                searchCtx is null)
            {
                return null;
            }

            int nv = src.VertexCount;
            ResultInfoDPtBary[] proj = new ResultInfoDPtBary[nv];
            int[] hitIndex = new int[nv];

            if (src.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> pclNrData) &&
               searchCtx.NearestSoa(pclNrData, nv, 1e3f, hitIndex.AsSpan(), proj.AsSpan()))
            {
                MeshBuilder mb = new MeshBuilder();
                List<Vector3> posProj = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);

                for (int i = 0; i < nv; i++)
                    posProj.Add(new Vector3(proj[i].x, proj[i].y, proj[i].z));

                return mb.ToPointCloud();
            }

            searchCtx.Dispose();
            return null;
        }

        public static PointCloud? ProjectWithRaycastNearest(PointCloud src, Mesh target, int gridSize = 16)
        {
            if (!src.HasSegment(MeshSegmentType.Normal))
                return null;

            if (WarpCoreStatus.WCORE_OK != SearchContext.TryInitTrigrid(target, gridSize, out SearchContext? searchCtx) ||
               searchCtx is null)
            {
                return null;
            }

            // TODO: optimize this, clamp searches on previous results

            int nv = src.VertexCount;
            ResultInfoDPtBary[] projNN = new ResultInfoDPtBary[nv];
            int[] hitIndexNN = new int[nv];

            float[] projRay0 = new float[nv];
            int[] hitIndexRay0 = new int[nv];

            float[] projRay1 = new float[nv];
            int[] hitIndexRay1 = new int[nv];

            if (src.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> pclNrPos) &&
                src.TryGetRawData(MeshSegmentType.Normal, -1, out ReadOnlySpan<byte> pclNrNormal))
            {
                ReadOnlySpan<float> pclNrPosF = MemoryMarshal.Cast<byte, float>(pclNrPos);
                ReadOnlySpan<float> pclNrNormF = MemoryMarshal.Cast<byte, float>(pclNrNormal);

                if (!searchCtx.NearestSoa(pclNrPos, nv, 1e3f, hitIndexNN.AsSpan(), projNN.AsSpan()) ||
                    !searchCtx.RaycastSoa(pclNrPos, pclNrNormal, nv, hitIndexRay0.AsSpan(), projRay0.AsSpan(), false) ||
                    !searchCtx.RaycastSoa(pclNrPos, pclNrNormal, nv, hitIndexRay1.AsSpan(), projRay1.AsSpan(), true))
                    return null;

                MeshBuilder mb = new MeshBuilder();
                List<Vector3> posProj = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);

                for (int i = 0; i < nv; i++)
                {
                    float bestRay = MathF.Min(projRay0[i], projRay1[i]);

                    Vector3 pt;
                    if (2 * bestRay < projNN[i].d)
                    {
                        pt = new Vector3(projNN[i].x, projNN[i].y, projNN[i].z);
                    }
                    else
                    {
                        Vector3 p0 = new Vector3(pclNrPosF[i], pclNrPosF[i + nv], pclNrPosF[i + 2 * nv]);
                        Vector3 n = new Vector3(pclNrNormF[i], pclNrNormF[i + nv], pclNrNormF[i + 2 * nv]);

                        if (projRay0[i] < projRay1[i])
                        {
                            pt = p0 + projRay0[i] * n;
                        }
                        else
                        {
                            pt = p0 - projRay1[i] * n;
                        }
                    }

                    posProj.Add(pt);
                }

                return mb.ToPointCloud();
            }

            searchCtx.Dispose();
            return null;
        }
    }
}
