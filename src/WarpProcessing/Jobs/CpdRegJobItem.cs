using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public class CpdRegJobItem : ProjectJobItem
    {
        public CpdRegJobItem(long specTableKey, string? gpaItem, string initItem, string meshColumn, int meshIndex, string result) :
            base("CPD registration", JobItemFlags.None)
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
                return false;

            PointCloud? pcl = ModelUtils.LoadSpecimenTableRef<PointCloud>(ctx.Project, column, MeshIndex);
            if (pcl is null)
                return false;

            if (GpaItem is not null)
            {
                if (!ctx.Workspace.TryGet(GpaItem, out Gpa? gpa) || gpa is null)
                    return false;

                Rigid3 transform = gpa.GetTransform(MeshIndex);
                pcl = RigidTransform.TransformPosition(pcl, transform);
            }

            if (!ctx.Workspace.TryGet(InitItem, out CpdContext? cpdContext) || cpdContext is null)
                return false;

            WarpCoreStatus regStatus = cpdContext.Register(pcl, out PointCloud? pclBent, out _);
            ctx.Workspace.Set(ResultItem, MeshIndex, pclBent);

            return regStatus == WarpCoreStatus.WCORE_OK;
        }
    }
}
