using System.IO;
using Warp9.Model;

namespace Warp9.ProjectExplorer
{
    public class Warp9Model
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

        public void Save(string fileName)
        {
            if (File.Exists(fileName))
            {
                throw new InvalidOperationException();
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

        //public void 
    }
}
