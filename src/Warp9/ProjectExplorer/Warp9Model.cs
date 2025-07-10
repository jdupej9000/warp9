using System.IO;
using System.Windows.Markup;
using Warp9.Model;

namespace Warp9.ProjectExplorer
{
    public class Warp9Model : IDisposable
    {
        public Warp9Model(Project project)
        {
            Project = project;
            ViewModel = new Warp9ViewModel(project);
            ViewModel.Update();
        }

        public Project Project { get; init; }
        public Warp9ViewModel ViewModel { get; init; }
        public bool IsDirty { get; private set; } = false;
        public string? FileName => Project.Archive?.FileName;

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
                Warp9ProjectArchive archSaved = new Warp9ProjectArchive(fileName, false);
                Project.SwitchToSavedArchive(archSaved);

                File.Delete(oldArchiveFileName);
            }
            else
            {
                Warp9ProjectArchive arch = new Warp9ProjectArchive(fileName, true);
                Project.Save(arch);
                arch.Dispose();

                Warp9ProjectArchive archSaved = new Warp9ProjectArchive(fileName, false);
                IProjectArchive? oldArch = Project.SwitchToSavedArchive(archSaved);
                oldArch?.Dispose();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            ViewModel.Dispose();
        }
    }
}
