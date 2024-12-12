using System.Security.Cryptography.Xml;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.JobItems
{
    public class SingleRigidRegJobItem : ProjectJobItem
    {
        public SingleRigidRegJobItem(int index, long specTableKey, string? gpaItem, string meshColumn, int meshIndex, string result, bool makeMesh=false) :
           base(index, "Rigid registration", JobItemFlags.None)
        {
            SpecimenTableKey = specTableKey;
            GpaItem = gpaItem;
            MeshColumn = meshColumn;
            MeshIndex = meshIndex;
            ResultItem = result;
            MakeMesh = makeMesh;
        }

        public long SpecimenTableKey { get; init; }
        public string? GpaItem { get; init; }
        public string MeshColumn { get; init; }
        public int MeshIndex { get; init; }
        public string ResultItem { get; init; }
        public bool MakeMesh { get; init; }

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

            PointCloud? pcl = ModelUtils.LoadSpecimenTableRef<Mesh>(ctx.Project, column, MeshIndex);
            if(pcl is null)
                pcl = ModelUtils.LoadSpecimenTableRef<PointCloud>(ctx.Project, column, MeshIndex);

            if (pcl is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                   string.Format("Cannot load mesh '{0}'.", MeshIndex));
                return false;
            }

            PointCloud? transformed = pcl;

            if (GpaItem is not null)
            {
                if (!ctx.Workspace.TryGet(GpaItem, out Gpa? gpa) || gpa is null)
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Error, "GPA is invalid.");
                    return false;
                }

                Rigid3 transform = gpa.GetTransform(MeshIndex);
                transformed = RigidTransform.TransformPosition(pcl, transform);
                if (transformed is null)
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Error, "Failed to transform mesh.");
                    return false;
                }
            }

            if(MakeMesh && pcl is Mesh mesh)
                ctx.Workspace.Set(ResultItem, MeshIndex, Mesh.FromPointCloud(transformed, mesh));
            else
                ctx.Workspace.Set(ResultItem, MeshIndex, transformed);

            ctx.WriteLog(ItemIndex, MessageKind.Information, "Applied rigid transform to mesh.");

            return true;
        }
    }
}
