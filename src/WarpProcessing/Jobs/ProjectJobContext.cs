using System.Diagnostics.CodeAnalysis;
using Warp9.Data;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public class ProjectJobContext : IJobContext
    {
        public ProjectJobContext(Project proj)
        {
            Project = proj;
        }

        public JobWorkspace Workspace { get; init; } = new JobWorkspace();
        public Project Project { get; init; }

        public bool TryGetSpecTableMesh(long specTableKey, string columnName, int index, [MaybeNullWhen(false)] out Mesh? m)
        {
            return TryGetSpecTableMeshRegistered(specTableKey, columnName, index, null, out m);
        }

        public bool TryGetSpecTableMeshRegistered(long specTableKey, string columnName, int index, string? gpaItem, [MaybeNullWhen(false)] out Mesh? m)
        {
            m = null;

            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                  Project, specTableKey, columnName);

            if (column is null)
                return false;

            Mesh? mesh = ModelUtils.LoadSpecimenTableRef<Mesh>(Project, column, index);
            if (mesh is null)
                return false;

            if (gpaItem is not null)
            {
                if (!Workspace.TryGet(gpaItem, out Gpa? gpa) || gpa is null)
                    return false;

                Rigid3 transform = gpa.GetTransform(index);
                PointCloud? transformed = RigidTransform.TransformPosition(mesh, transform);
                if (transformed is null)
                    return false;

                mesh = Mesh.FromPointCloud(transformed, mesh);
            }

            m = mesh;
            return true;
        }
    }
}
