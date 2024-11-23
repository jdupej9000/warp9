using Warp9.Data;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public class CpdRegJobItem : ProjectJobItem
    {
        public CpdRegJobItem(int index, long specTableKey, string? gpaItem, string initItem, string meshColumn, int meshIndex, string result) :
            base(index, "CPD registration", JobItemFlags.None)
        {
            SpecimenTableKey = specTableKey;
            GpaItem = gpaItem;
            InitItem = initItem;
            MeshColumn = meshColumn;
            MeshIndex = meshIndex;
            ResultItem = result;
        }

        public long SpecimenTableKey { get; init; }
        public string? GpaItem { get; init; }
        public string InitItem { get; init; }
        public string MeshColumn { get; init; }
        public int MeshIndex { get; init; }
        public string ResultItem { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                  ctx.Project, SpecimenTableKey, MeshColumn);
            if (column is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Cannot find column '{0}' in entity '{1}'.", MeshColumn, SpecimenTableKey));
                return false;
            }

            PointCloud? pcl = ModelUtils.LoadSpecimenTableRef<PointCloud>(ctx.Project, column, MeshIndex);
            if (pcl is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Cannot load point cloud '{0}'.", MeshIndex));
                return false;
            }

            if (GpaItem is not null)
            {
                if (!ctx.Workspace.TryGet(GpaItem, out Gpa? gpa) || gpa is null)
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Error, "GPA is invalid.");
                    return false;
                }

                Rigid3 transform = gpa.GetTransform(MeshIndex);
                pcl = RigidTransform.TransformPosition(pcl, transform);
            }

            if (!ctx.Workspace.TryGet(InitItem, out CpdContext? cpdContext) || cpdContext is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "CPD-LR initialization is invalid.");
                return false;
            }

            WarpCoreStatus regStatus = cpdContext.Register(pcl, out PointCloud? pclBent, out _);
            ctx.Workspace.Set(ResultItem, MeshIndex, pclBent);

            ctx.WriteLog(ItemIndex, MessageKind.Information, "CPD registration complete: " + regStatus.ToString());

            return regStatus == WarpCoreStatus.WCORE_OK;
        }
    }
}
