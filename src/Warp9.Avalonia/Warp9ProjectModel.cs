using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Warp9.Model;

namespace Warp9.Avalonia
{
    

    public class Warp9ProjectModel
    {
        public Warp9ProjectModel(Project proj)
        {
            Project = proj;
            ProjectItems.Add(new GeneralProjectItem(this));
            ProjectItems.Add(new DatasetsProjectItem(this));
            ProjectItems.Add(new ResultsProjectItem(this));
        }

        public Project Project { get; init; }

        public ObservableCollection<ProjectItem> ProjectItems { get; private set; } = new ObservableCollection<ProjectItem>();
    }
}
