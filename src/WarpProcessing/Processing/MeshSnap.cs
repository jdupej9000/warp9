using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Native;
using Warp9.Utils;

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
            ResultInfoDPtBary[] proj = ArrayPool<ResultInfoDPtBary>.Shared.Rent(nv);
            int[] hitIndex = ArrayPool<int>.Shared.Rent(nv);

            if (src.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> pclNrData) &&
               searchCtx.NearestSoa(pclNrData, nv, 1e3f, hitIndex.AsSpan(), proj.AsSpan()))
            {
                MeshBuilder mb = new MeshBuilder();
                List<Vector3> posProj = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);

                for (int i = 0; i < nv; i++)
                    posProj.Add(new Vector3(proj[i].x, proj[i].y, proj[i].z));

                return mb.ToPointCloud();
            }

            ArrayPool<ResultInfoDPtBary>.Shared.Return(proj);
            ArrayPool<int>.Shared.Return(hitIndex);
            searchCtx.Dispose();
            return null;
        }

        public static PointCloud? ProjectWithRaycastNearest(PointCloud src, Mesh target, int gridSize = 16)
        {
            if (!src.HasSegment(MeshSegmentSemantic.Normal))
                return null;

            if (WarpCoreStatus.WCORE_OK != SearchContext.TryInitTrigrid(target, gridSize, out SearchContext? searchCtx) ||
               searchCtx is null)
            {
                return null;
            }

            // TODO: optimize this, clamp searches on previous results

            int nv = src.VertexCount;
            ResultInfoDPtBary[] projNN = ArrayPool<ResultInfoDPtBary>.Shared.Rent(nv);
            int[] hitIndexNN = ArrayPool<int>.Shared.Rent(nv);

            float[] projRay0 = ArrayPool<float>.Shared.Rent(nv);
            int[] hitIndexRay0 = ArrayPool<int>.Shared.Rent(nv);

            float[] projRay1 = ArrayPool<float>.Shared.Rent(nv);
            int[] hitIndexRay1 = ArrayPool<int>.Shared.Rent(nv);

            if (src.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> pclNrPos) &&
                src.TryGetRawData(MeshSegmentSemantic.Normal, -1, out ReadOnlySpan<byte> pclNrNormal))
            {
                ReadOnlySpan<float> pclNrPosF = MemoryMarshal.Cast<byte, float>(pclNrPos);
                ReadOnlySpan<float> pclNrNormF = MemoryMarshal.Cast<byte, float>(pclNrNormal);

                if (!searchCtx.NearestSoa(pclNrPos, nv, 1e3f, hitIndexNN.AsSpan(), projNN.AsSpan()) ||
                    !searchCtx.RaycastSoa(pclNrPos, pclNrNormal, nv, hitIndexRay0.AsSpan(), projRay0.AsSpan(), false) ||
                    !searchCtx.RaycastSoa(pclNrPos, pclNrNormal, nv, hitIndexRay1.AsSpan(), projRay1.AsSpan(), true))
                    return null;

                MeshBuilder mb = new MeshBuilder();
                List<Vector3> posProj = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);

                for (int i = 0; i < nv; i++)
                {
                    float ray0 = (hitIndexRay0[i] >= 0) ? projRay0[i] : float.MaxValue;
                    float ray1 = (hitIndexRay1[i] >= 0) ? projRay1[i] : float.MaxValue;
                    float bestRay = MathF.Min(ray0, ray1);

                    Vector3 pt;
                    if (bestRay > 3 * projNN[i].d)
                    {
                        pt = new Vector3(projNN[i].x, projNN[i].y, projNN[i].z);
                    }
                    else
                    {
                        Vector3 p0 = new Vector3(pclNrPosF[i], pclNrPosF[i + nv], pclNrPosF[i + 2 * nv]);
                        Vector3 n = new Vector3(pclNrNormF[i], pclNrNormF[i + nv], pclNrNormF[i + 2 * nv]);

                        if (ray0 < ray1)                       
                            pt = p0 + ray0 * n;                      
                        else
                            pt = p0 - ray1 * n;
                    }

                    posProj.Add(pt);
                }

                return mb.ToPointCloud();
            }

            ArrayPool<float>.Shared.Return(projRay0);
            ArrayPool<float>.Shared.Return(projRay1);
            ArrayPool<int>.Shared.Return(hitIndexRay0);
            ArrayPool<int>.Shared.Return(hitIndexRay1);
            ArrayPool<int>.Shared.Return(hitIndexNN);
            ArrayPool<ResultInfoDPtBary>.Shared.Return(projNN);

            searchCtx.Dispose();
            return null;
        }

        public static PointCloud? SymmetricSnap(PointCloud original, PointCloud regMirror, IFaceCollection faces)
        {
            const int GridSize = 16;
            Mesh meshOriginal = Mesh.FromPointCloud(original, faces);
            Mesh meshMirror = Mesh.FromPointCloud(regMirror, faces);

            int nv = original.VertexCount;

            if (SearchContext.TryInitTrigrid(meshOriginal, GridSize, out SearchContext? searchOriginal) == WarpCoreStatus.WCORE_OK &&
                SearchContext.TryInitTrigrid(meshMirror, GridSize, out SearchContext? searchMirror) == WarpCoreStatus.WCORE_OK &&
                searchOriginal is not null &&
                searchMirror is not null &&
                original.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> rawOriginal) &&
                regMirror.TryGetRawData(MeshSegmentSemantic.Position, -1, out ReadOnlySpan<byte> rawMirror) &&
                faces.TryGetIndexData(out ReadOnlySpan<FaceIndices> indices))
            {
                MeshBuilder mb = new MeshBuilder();
                List<Vector3> posProj = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);
                posProj.Capacity = nv;

                int[] projIdxOrig = ArrayPool<int>.Shared.Rent(nv);
                ResultInfoDPtBary[] projOrig = ArrayPool<ResultInfoDPtBary>.Shared.Rent(nv);
                searchMirror.NearestSoa(rawOriginal, nv, 1000, projIdxOrig, projOrig);

                int[] projIdxMirror = ArrayPool<int>.Shared.Rent(nv);
                ResultInfoDPtBary[] projMirror = ArrayPool<ResultInfoDPtBary>.Shared.Rent(nv);
                searchOriginal.NearestSoa(rawMirror, nv, 1000, projIdxMirror, projMirror);


                for (int i = 0; i < nv; i++)
                {
                    // Just project the orig point onto the mirror mesh.
                    Vector3 s0 = MiscUtils.SampleTriangleBarycentric(rawMirror, indices[projIdxOrig[i]], nv, projOrig[i].u, projOrig[i].v);

                    // Project the mirror point with the same index onto the original mesh. Use the hit barycentric
                    // coordinates to sample the mirror mesh.
                    Vector3 s1 = MiscUtils.SampleTriangleBarycentric(rawMirror, indices[projIdxMirror[i]], nv, projMirror[i].u, projOrig[i].v);

                    // Blend the two guesses.
                    Vector3 ptm = Vector3.Lerp(s0, s1, 0.5f);

                    // Blend with the original point to get symmetry.
                    Vector3 pt = Vector3.Lerp(MiscUtils.FromSoa(rawOriginal, i, nv), ptm, 0.5f);

                    posProj.Add(pt);
                }

                ArrayPool<int>.Shared.Return(projIdxOrig);
                ArrayPool<ResultInfoDPtBary>.Shared.Return(projOrig);
                ArrayPool<int>.Shared.Return(projIdxMirror);
                ArrayPool<ResultInfoDPtBary>.Shared.Return(projMirror);

                return mb.ToPointCloud();
            }

            return null;
        }
    }
}
