﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.JobItems
{
    public class SurfaceProjectionJobItem : ProjectJobItem
    {
        public SurfaceProjectionJobItem(int index, long specTableKey, string meshCol, int meshIndex, int baseMeshIndex, string nonrigidItem, string? gpaItem, bool useRayCast, string resultItem) :
            base(index, "Surface projection", JobItemFlags.FailuesAreFatal)
        {
            SpecimenTableKey = specTableKey;
            MeshColumn = meshCol;
            MeshIndex = meshIndex;
            BaseMeshIndex = baseMeshIndex;
            NonrigidMeshesItem = nonrigidItem;
            GpaItem = gpaItem;
            ResultItem = resultItem;
            UseRaycast = useRayCast;
        }

        public int MeshIndex { get; init; }
        public int BaseMeshIndex { get; init; }
        public string NonrigidMeshesItem { get; init; }
        public string? GpaItem { get; init; }
        public long SpecimenTableKey { get; init; }
        public string ResultItem { get; init; }
        public string MeshColumn { get; init; }
        public bool UseRaycast { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            const int GridSize = 16;

            if (!ctx.TryGetSpecTableMeshRegistered(SpecimenTableKey, MeshColumn, MeshIndex, GpaItem, out Mesh? floatingMesh) ||
                floatingMesh is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                   string.Format("Cannot load transformed mesh '{0}'.", MeshIndex));
                return false;
            }

            if (!ctx.Workspace.TryGet(NonrigidMeshesItem, MeshIndex, out PointCloud? pclNonrigid) || pclNonrigid is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Could not load the bent floating mesh '{0}'.", MeshIndex));
                return false;
            }

            PointCloud? pclProj;
            if (UseRaycast)
            {
                if (!ctx.TryGetSpecTableMeshRegistered(SpecimenTableKey, MeshColumn, BaseMeshIndex, null, out Mesh? baseMesh) || baseMesh is null)
                    return false;

                Mesh nonrigidWithNorm = MeshNormals.MakeNormals(Mesh.FromPointCloud(pclNonrigid, baseMesh), NormalsAlgorithm.FastRobust);
                pclProj = MeshSnap.ProjectWithRaycastNearest(MeshNormals.MakeNormals(nonrigidWithNorm), floatingMesh, GridSize);
            }
            else
            {
                pclProj = MeshSnap.ProjectToNearest(pclNonrigid, floatingMesh, GridSize);           
            }

            if (pclProj is not null)
            {
                ctx.Workspace.Set(ResultItem, MeshIndex, pclProj);
                ctx.WriteLog(ItemIndex, MessageKind.Information, "Surface projection complete.");
                return true;
            }

            ctx.WriteLog(ItemIndex, MessageKind.Error, "Spatial searching failed.");
            return false;
        }
    }
}
