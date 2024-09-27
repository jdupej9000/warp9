using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    }
}
