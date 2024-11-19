using Warp9.Data;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public class SingleRigidRegJobItem : ProjectJobItem
    {
        public SingleRigidRegJobItem(long specTableKey, string? gpaItem, string meshColumn, int meshIndex, string result) :
           base("Rigid registration", JobItemFlags.None)
        {
            SpecimenTableKey = specTableKey;
            GpaItem = gpaItem;
            MeshColumn = meshColumn;
            MeshIndex = meshIndex;
            ResultItem = result;
        }

        public long SpecimenTableKey { get; init; }
        public string? GpaItem { get; init; }
        public string MeshColumn { get; init; }
        public int MeshIndex { get; init; }
        public string ResultItem { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                   ctx.Project, SpecimenTableKey, MeshColumn);
            if (column is null)
                return false;

            Mesh? mesh = ModelUtils.LoadSpecimenTableRef<Mesh>(ctx.Project, column, MeshIndex);
            if (mesh is null)
                return false;

            if (GpaItem is not null)
            {
                if (!ctx.Workspace.TryGet(GpaItem, out Gpa? gpa) || gpa is null)
                    return false;

                Rigid3 transform = gpa.GetTransform(MeshIndex);
                PointCloud? transformed = RigidTransform.TransformPosition(mesh, transform);
                if (transformed is null) 
                    return false;

                mesh = Mesh.FromPointCloud(transformed, mesh);
            }

            ctx.Workspace.Set(ResultItem, MeshIndex, mesh);
            return true;
        }
    }
}
