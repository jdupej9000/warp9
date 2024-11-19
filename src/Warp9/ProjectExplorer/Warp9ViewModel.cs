using System.Collections.ObjectModel;
using Warp9.Model;

namespace Warp9.ProjectExplorer
{
    public enum StockProjectItemKind
    {
        None,

        General,
        GeneralComment,
        GeneralSettings,
        Datasets,
        Results,
        Galleries,

        Entry
    }

    public class ProjectItem
    {
        public string Name { get; set; } = string.Empty;
        public long EntryIndex { get; set; } = -1;
        public int ViewIndex { get; set; } = -1;
        public StockProjectItemKind Kind {get; set;} = StockProjectItemKind.None;
        public ObservableCollection<ProjectItem> Children { get; set; } = new ObservableCollection<ProjectItem>();

        public static ProjectItem CreateStock(string name, StockProjectItemKind kind)
        {
            return new ProjectItem()
            {
                Name = name,
                Kind = kind
            };
        }

        public static ProjectItem CreateEntry(string name, long entryIndex)
        {
            return new ProjectItem()
            {
                Name = name,
                Kind = StockProjectItemKind.Entry,
                EntryIndex = entryIndex
            };
        }
    }

    public class Warp9ViewModel
    {
        public Warp9ViewModel(Project project)
        {
            Project = project;
        }

        const int ProjectItemIdxGeneral = 0;
        const int ProjectItemIdxDatasets = 1;
        const int ProjectItemIdxResults = 2;
        const int ProjectItemIdxGalleries = 3;

        public ObservableCollection<ProjectItem> Items { get; init; } = new ObservableCollection<ProjectItem>();
        public Project Project { get; init; }

        public void Update()
        {
            UpdateProjectItems();
        }

        private void UpdateProjectItems()
        {
            Items.Clear();

            // Must add these in correct order so that ProjectItemIdxGeneral and the like work right.
            Items.Add(ProjectItem.CreateStock("General", StockProjectItemKind.General));
            Items.Add(ProjectItem.CreateStock("Data sets", StockProjectItemKind.Datasets));
            Items.Add(ProjectItem.CreateStock("Results", StockProjectItemKind.Results));
            Items.Add(ProjectItem.CreateStock("Galleries", StockProjectItemKind.Galleries));

            Items[ProjectItemIdxGeneral].Children.Add(
                ProjectItem.CreateStock("Comment", StockProjectItemKind.GeneralComment));
            Items[ProjectItemIdxGeneral].Children.Add(
               ProjectItem.CreateStock("Settings", StockProjectItemKind.GeneralSettings));


            foreach (var kvp in Project.Entries)
            {
                if (kvp.Value.Kind == ProjectEntryKind.Specimens)
                {
                    ProjectItem pi = ProjectItem.CreateEntry(kvp.Value.Name, kvp.Key);
                    Items[ProjectItemIdxDatasets].Children.Add(pi);
                }
                else if (kvp.Value.Kind == ProjectEntryKind.MeshCorrespondence)
                {
                    ProjectItem pi = ProjectItem.CreateEntry(kvp.Value.Name, kvp.Key);
                    Items[ProjectItemIdxResults].Children.Add(pi);
                }
            }

        }
    }
}
