using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public class SurfaceProjectionJobItem : ProjectJobItem
    {
        public SurfaceProjectionJobItem(long specTableKey, string meshCol, int meshIndex, string nonrigidItem, string? gpaItem, string resultItem) :
            base("Surface projection", JobItemFlags.FailuesAreFatal)
        {
            SpecimenTableKey = specTableKey;
            MeshColumn = meshCol;
            MeshIndex = meshIndex;
            NonrigidMeshesItem = nonrigidItem;
            GpaItem = gpaItem;
            ResultItem = resultItem;
        }

        public int MeshIndex { get; init; }
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

            if(WarpCoreStatus.WCORE_OK != SearchContext.TryInitTrigrid(floatingMesh, 1, out SearchContext? searchCtx) || searchCtx is null)
                return false;

            if (!ctx.Workspace.TryGet(NonrigidMeshesItem, MeshIndex, out PointCloud? pclNonrigid))
                return false;

            // TODO

            searchCtx.Dispose();

            throw new NotImplementedException();
        }
    }
}
