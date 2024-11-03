using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public class CpdInitJobItem : ProjectJobItem
    {
        public CpdInitJobItem(long specTableKey, int baseIndex, string meshColumn, string result) :
            this(specTableKey, null, baseIndex, meshColumn, result)
        { }

        public CpdInitJobItem(long specTableKey, string? gpaItem, int baseIndex, string meshColumn, string result) :
            base("CPD initialization", JobItemFlags.FailuesAreFatal | JobItemFlags.RunsAlone)
        {
            SpecimenTableKey = specTableKey;
            GpaItem = gpaItem;
            BaseMeshIndex = baseIndex;
            InitObjectItem = result;
            MeshColumn = meshColumn;
        }

        public long SpecimenTableKey { get; init; }
        public string? GpaItem { get; init; }
        public string MeshColumn { get; init; }
        public int BaseMeshIndex { get; init; }
        public string InitObjectItem { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                ctx.Project, SpecimenTableKey, MeshColumn);
            if (column is null)
                return false;

            PointCloud? baseMesh = ModelUtils.LoadSpecimenTableRef<Mesh>(ctx.Project, column, BaseMeshIndex);
            if (baseMesh is null) 
                return false;

            if (GpaItem is not null)
            {
                if (!ctx.Workspace.TryGet(GpaItem, out Gpa? gpa) || gpa is null)
                    return false;

                Rigid3 transform = gpa.GetTransform(BaseMeshIndex);
                baseMesh = RigidTransform.TransformPosition(baseMesh, transform);
                if (baseMesh is null)
                    return false;
            }

            WarpCoreStatus initStat = CpdContext.TryInitNonrigidCpd(out CpdContext? cpdCtx, baseMesh);
            if (initStat != WarpCoreStatus.WCORE_OK || cpdCtx is null)
                return false;

            ctx.Workspace.Set(InitObjectItem, 0, cpdCtx);
            return true;
        }
    }
}
