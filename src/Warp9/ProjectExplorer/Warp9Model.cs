using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;

namespace Warp9.ProjectExplorer
{
    public class ProjectItem
    {
        public ProjectItem(string name)
        {
            Name = name;
        }

        public ProjectItem(string name, int entryIdx)
        {
            Name = name;
            Level = 1;
            EntryIndex = entryIdx;
        }

        public ProjectItem(string name, int entryIdx, int viewIndex)
        {
            Name = name;
            Level = 2;
            EntryIndex = entryIdx;
            ViewIndex = viewIndex;
        }

        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } = 0;
        public int EntryIndex { get; set; } = -1;
        public int ViewIndex { get; set; } = -1;
        public ObservableCollection<ProjectItem> Children { get; set; } = new ObservableCollection<ProjectItem>();
    }

    public class Warp9Model
    {
        public Warp9Model(Project project)
        {
            Project = project;
            UpdateProjectItems();
        }

        public Project Project { get; init; }
        public bool IsDirty { get; private set; } = false;

        const int ProjectItemIdxGeneral = 0;
        const int ProjectItemIdxDatasets = 1;
        const int ProjectItemIdxResults = 2;
        const int ProjectItemIdxGalleries = 3;
        public ObservableCollection<ProjectItem> Items { get; init; } = new ObservableCollection<ProjectItem>();


        private void UpdateProjectItems()
        {
            Items.Clear();
            Items.Add(new ProjectItem("General"));
            Items[ProjectItemIdxGeneral].Children.Add(new ProjectItem("Comment", ProjectItemIdxGeneral));
            Items.Add(new ProjectItem("Datasets"));
            Items.Add(new ProjectItem("Results"));
            Items.Add(new ProjectItem("Galleries"));

            foreach (var kvp in Project.Entries)
            {
                if (kvp.Value.Kind == ProjectEntryKind.Specimens)
                {
                    ProjectItem pi = new ProjectItem(kvp.Value.Name, kvp.Key);
                    Items[ProjectItemIdxDatasets].Children.Add(pi);
                }
                else if (kvp.Value.Kind == ProjectEntryKind.MeshCorrespondence)
                {
                    ProjectItem pi = new ProjectItem(kvp.Value.Name, kvp.Key);
                    Items[ProjectItemIdxResults].Children.Add(pi);
                }
            }

        }
    }
}
