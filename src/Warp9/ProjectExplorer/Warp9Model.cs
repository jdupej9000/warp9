using System.IO;
using System.Windows.Markup;
using Warp9.Jobs;
using Warp9.Model;

namespace Warp9.ProjectExplorer
{
    public enum ModelEventKind
    {
        JobStarting,
        ProjectSaved
    }

    public record ModelEventInfo(ModelEventKind Kind, string? FileName=null);

    public class Warp9Model : IWarp9Model, IDisposable
    {
        public Warp9Model(Project project, int numWorkerThreads=-1)
        {
            JobEngine = new JobEngine(numWorkerThreads);
            Project = project;
            ViewModel = new Warp9ViewModel(project, this);
            ViewModel.Update();
        }

        public Project Project { get; init; }
        public Warp9ViewModel ViewModel { get; init; }
        public bool IsDirty { get; private set; } = false;
        public string? FileName => Project.Archive?.FileName;
        public JobEngine JobEngine { get; init; }

        public event EventHandler<string> LogMessage;
        public event EventHandler<ModelEventInfo> ModelEvent;

        public void Save(string fileName)
        {
            if (File.Exists(fileName))
            {
                string tempArchiveFileName = fileName + ".w9temp";
                Warp9ProjectArchive arch = new Warp9ProjectArchive(tempArchiveFileName, true);
                Project.Save(arch);
                arch.Dispose();

                IProjectArchive? oldArch = Project.SwitchToSavedArchive(null);
                oldArch?.Dispose();

                string oldArchiveFileName = fileName + ".w9old";
                File.Move(fileName, oldArchiveFileName, true);

                File.Move(tempArchiveFileName, fileName);
                Warp9ProjectArchive archSaved = new Warp9ProjectArchive(fileName, false, Options.Instance.NumWorkerThreads > 1);
                Project.SwitchToSavedArchive(archSaved);

                File.Delete(oldArchiveFileName);
            }
            else
            {
                Warp9ProjectArchive arch = new Warp9ProjectArchive(fileName, true);
                Project.Save(arch);
                arch.Dispose();

                Warp9ProjectArchive archSaved = new Warp9ProjectArchive(fileName, false, Options.Instance.NumWorkerThreads > 1);
                IProjectArchive? oldArch = Project.SwitchToSavedArchive(archSaved);
                oldArch?.Dispose();
            }

            ModelEvent?.Invoke(this, new ModelEventInfo(ModelEventKind.ProjectSaved, fileName));
        }

        public void StartJob(IEnumerable<IJobItem> items, string? title = null)
        {
            ProjectJobContext ctx = new ProjectJobContext(Project);
            ctx.LogMessage += OnJobMessage;

            Job job = Job.Create(items, ctx, title);
            ModelEvent?.Invoke(this, new ModelEventInfo(ModelEventKind.JobStarting));
            JobEngine.Run(job);
        }

        public void Dispose()
        {
            JobEngine.Dispose();
            GC.SuppressFinalize(this);
            ViewModel.Dispose();
        }

        private void OnJobMessage(object? sender, string msg)
        {
            LogMessage?.Invoke(sender, msg);
        }
    }
}
