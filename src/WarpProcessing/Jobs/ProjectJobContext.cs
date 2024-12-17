using System;
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

        public event EventHandler<string> LogMessage;

        public JobWorkspace Workspace { get; init; } = new JobWorkspace();
        public Project Project { get; init; }

        public void WriteLog(int jobItemIndex, MessageKind kind, string message)
        {
            string fmtMsg;

            if (jobItemIndex < 0)
                fmtMsg = string.Format("{0}: {1}",
                    DateTime.Now.ToString("HH:mm:ss.fff"),
                    message);
            else
                fmtMsg = string.Format("{0}: {1}> {2}",
                    DateTime.Now.ToString("HH:mm:ss.fff"),
                    jobItemIndex, message);

            if (LogMessage != null)
                LogMessage(this, fmtMsg);
            else
                Console.WriteLine(fmtMsg);
        }

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
