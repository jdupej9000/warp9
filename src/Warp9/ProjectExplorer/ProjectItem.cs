using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Warp9.Model;
using Warp9.Navigation;
using Warp9.Viewer;

namespace Warp9.ProjectExplorer
{
    public class ProjectItem
    {
        protected ProjectItem(Warp9ViewModel vm, Type? presenterType)
        {
            ParentViewModel = vm;
            PagePresenterType = presenterType;
        }

        public string Name { get; set; } = string.Empty;
        public ObservableCollection<ProjectItem> Children { get; set; } = new ObservableCollection<ProjectItem>();
        public Warp9ViewModel ParentViewModel { get; init; }
        public Type? PagePresenterType { get; init; }
        public virtual void ConfigurePresenter(IWarp9View pres) { }

        public virtual void Update()
        {
            foreach (ProjectItem pi in Children)
                pi.Update();
        }
    }

    public class GeneralProjectItem : ProjectItem
    {
        public GeneralProjectItem(Warp9ViewModel vm)
            : base(vm, null)
        {
            Name = "General";
            Children.Add(new GeneralCommentProjectItem(vm));
            Children.Add(new GeneralSettingsProjectItem(vm));
        }
    }

    public class GeneralCommentProjectItem : ProjectItem
    {
        public GeneralCommentProjectItem(Warp9ViewModel vm) :
            base(vm, typeof(TextEditorPage))
        {
            Name = "Comment";
        }

        public override void ConfigurePresenter(IWarp9View pres)
        {
            if (pres is not TextEditorPage page)
                throw new ArgumentException();
        }
    }

    public class GeneralSettingsProjectItem : ProjectItem
    {
        public GeneralSettingsProjectItem(Warp9ViewModel vm) :
            base(vm, typeof(ProjectSettingsPage))
        {
            Name = "Settings";
        }

        public override void ConfigurePresenter(IWarp9View pres)
        {
            if (pres is not ProjectSettingsPage page)
                throw new ArgumentException();
        }
    }

    public class DatasetsProjectItem : ProjectItem
    {
        public DatasetsProjectItem(Warp9ViewModel vm) :
            base(vm, null)
        {
            Name = "Datasets";
            Update();
        }

        public override void Update()
        {
            List<SpecimenTableInfo> tables = ModelUtils.EnumerateSpecimenTables(ParentViewModel.Project).ToList();

            // TODO: do not remove items that were not changed
            Children.Clear();
            foreach (SpecimenTableInfo sti in tables)
                Children.Add(new SpecimenTableProjectItem(ParentViewModel, sti.SpecTableId));

            base.Update();
        }
    }

    public class SpecimenTableProjectItem : ProjectItem
    {
        public SpecimenTableProjectItem(Warp9ViewModel vm, long key, string? explicitName=null) :
            base(vm, typeof(SpecimenTablePage))
        {
            Key = key;
            ExplicitName = explicitName;
        }

        public long Key { get; init; }
        public string? ExplicitName { get; init; }

        public override void ConfigurePresenter(IWarp9View pres)
        {
            if (pres is not SpecimenTablePage page)
                throw new ArgumentException();

            page.ShowEntry(Key);
        }

        public override void Update()
        {
            if (ParentViewModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                Name = ExplicitName ?? entry.Name;
            else
                Name = "(error)";
        }
    }

    public class ResultsProjectItem : ProjectItem
    {
        public ResultsProjectItem(Warp9ViewModel vm) :
            base(vm, null)
        {
            Name = "Results";
            Update();
        }

        public override void Update()
        {
            Children.Clear();

            foreach (var kvp in ParentViewModel.Project.Entries
                .Where((e) => e.Value.Kind == ProjectEntryKind.MeshCorrespondence))
            {
                Children.Add(new MeshCorrespondenceProjectItem(ParentViewModel, kvp.Key));
            }   

            base.Update();
        }
    }

    public class MeshCorrespondenceProjectItem : ProjectItem
    {
        public MeshCorrespondenceProjectItem(Warp9ViewModel vm, long key) :
            base(vm, typeof(ViewerPage))
        {
            Key = key;
            Children.Add(new SpecimenTableProjectItem(vm, key, "Results table"));
        }

        public long Key { get; init; }

        public override void Update()
        {
            if (ParentViewModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                Name = entry.Name;
            else
                Name = "(error)";

            base.Update();
        }

        public override void ConfigurePresenter(IWarp9View pres)
        {
            base.ConfigurePresenter(pres);

            if (pres is not ViewerPage page)
                throw new ArgumentException();

            Project proj = ParentViewModel.Project;
            page.SetContent(
                new CorrMeshViewerContent(proj, Key, "Correspondence meshes"),
                new CompareGroupsViewerContent(proj, Key, "Compare groups"));
        }
    }
}