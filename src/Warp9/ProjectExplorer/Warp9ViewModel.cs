using System.Collections.ObjectModel;
using System.ComponentModel;
using Warp9.Model;

namespace Warp9.ProjectExplorer
{
    public class Warp9ViewModel : INotifyPropertyChanged
    {
        public Warp9ViewModel(Project project) 
        {
            Project = project;

            Items.Add(new GeneralProjectItem(this));
            Items.Add(new DatasetsProjectItem(this));
            Items.Add(new ResultsProjectItem(this));
        }

        public ObservableCollection<ProjectItem> Items { get; init; } = new ObservableCollection<ProjectItem>();
        public Project Project { get; init; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Update()
        {
            foreach (ProjectItem pi in Items)
                pi.Update();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Items"));
        }
    }
}
