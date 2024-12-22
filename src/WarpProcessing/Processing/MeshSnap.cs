using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
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
    }
}
