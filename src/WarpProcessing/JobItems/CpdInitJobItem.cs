using System.Transactions;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.JobItems
{
    public class CpdInitJobItem : ProjectJobItem
    {
        public CpdInitJobItem(int index, long specTableKey, int baseIndex, string meshColumn, string result) :
            this(index, specTableKey, null, baseIndex, meshColumn, result)
        { }

        public CpdInitJobItem(int index, long specTableKey, string? gpaItem, int baseIndex, string meshColumn, string result) :
            base(index, "CPD initialization", JobItemFlags.FailuesAreFatal | JobItemFlags.RunsAlone)
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
            ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Initializing CPD for mesh '{0}'.", BaseMeshIndex));

            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                ctx.Project, SpecimenTableKey, MeshColumn);
            if (column is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Cannot find column '{0}' in entity '{1}'.", MeshColumn, SpecimenTableKey));
                return false;
            }

            PointCloud? baseMesh = ModelUtils.LoadSpecimenTableRef<Mesh>(ctx.Project, column, BaseMeshIndex);
            if (baseMesh is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "Cannot load base mesh.");
                return false;
            }

            if (GpaItem is not null)
            {
                if (!ctx.Workspace.TryGet(GpaItem, out Gpa? gpa) || gpa is null)
                    return false;

                Rigid3 transform = gpa.GetTransform(BaseMeshIndex);
                baseMesh = RigidTransform.TransformPosition(baseMesh, transform);
                if (baseMesh is null)
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Error, "Failed to transform base mesh.");
                    return false;
                }
            }

            CpdConfiguration cpdCfg = new CpdConfiguration();
            cpdCfg.UseGpu = true;

            WarpCoreStatus initStat = CpdContext.TryInitNonrigidCpd(out CpdContext? cpdCtx, baseMesh, cpdCfg);
            if (initStat != WarpCoreStatus.WCORE_OK || cpdCtx is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "CPD-LR initialization failed.");
                return false;
            }

            ctx.Workspace.Set(InitObjectItem, cpdCtx);
            ctx.WriteLog(ItemIndex, MessageKind.Information, 
                string.Format("CPD-LR initialization complete (m={0}, eigs={1}).", cpdCtx.NumVertices, cpdCtx.NumEigenvectors));
            return true;
        }
    }
}
