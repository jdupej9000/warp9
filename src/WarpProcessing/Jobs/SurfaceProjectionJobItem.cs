using System;
using System.Collections.Generic;
using System.Numerics;
using Warp9.Data;
using Warp9.Native;

namespace Warp9.Jobs
{
    public class SurfaceProjectionJobItem : ProjectJobItem
    {
        public SurfaceProjectionJobItem(int index, long specTableKey, string meshCol, int meshIndex, int baseMeshIndex, string nonrigidItem, string? gpaItem, string resultItem) :
            base(index, "Surface projection", JobItemFlags.FailuesAreFatal)
        {
            SpecimenTableKey = specTableKey;
            MeshColumn = meshCol;
            MeshIndex = meshIndex;
            BaseMeshIndex = baseMeshIndex;
            NonrigidMeshesItem = nonrigidItem;
            GpaItem = gpaItem;
            ResultItem = resultItem;
        }

        public int MeshIndex { get; init; }
        public int BaseMeshIndex { get; init; }
        public string NonrigidMeshesItem { get; init; }
        public string? GpaItem { get; init; }
        public long SpecimenTableKey { get; init; }
        public string ResultItem { get; init; }
        public string MeshColumn { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            if(!ctx.TryGetSpecTableMeshRegistered(SpecimenTableKey, MeshColumn, MeshIndex, GpaItem, out Mesh? floatingMesh) || 
                floatingMesh is null)
                return false;

            // we need this to copy triangle indices from (only the reference is copied)
            if (!ctx.TryGetSpecTableMesh(SpecimenTableKey, MeshColumn, MeshIndex, out Mesh? baseMesh) ||
                baseMesh is null)
                return false;

            if (WarpCoreStatus.WCORE_OK != SearchContext.TryInitTrigrid(floatingMesh, 16, out SearchContext? searchCtx) || searchCtx is null)
                return false;

            if (!ctx.Workspace.TryGet(NonrigidMeshesItem, MeshIndex, out PointCloud? pclNonrigid) || pclNonrigid is null)
                return false;

            int nv = pclNonrigid.VertexCount;
            ResultInfoDPtBary[] proj = new ResultInfoDPtBary[nv];
            int[] hitIndex = new int[nv];

            bool ret = false;
            if (pclNonrigid.TryGetRawData(MeshSegmentType.Position, -1, out ReadOnlySpan<byte> pclNrData) &&
                searchCtx.NearestSoa(pclNrData, nv, 1e3f, hitIndex.AsSpan(), proj.AsSpan()))
            {
                MeshBuilder mb = new MeshBuilder();
                List<Vector3> posProj = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);

                for (int i = 0; i < nv; i++)
                    posProj.Add(new Vector3(proj[i].x, proj[i].y, proj[i].z));

                Mesh corrMesh = Mesh.FromPointCloud(mb.ToPointCloud(), baseMesh);
                ctx.Workspace.Set(ResultItem, MeshIndex, corrMesh);

                ret = true;
            }

            searchCtx.Dispose();

            return ret;
        }
    }
}
