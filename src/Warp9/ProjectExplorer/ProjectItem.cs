using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Warp9.Data;
using Warp9.Model;
using Warp9.Navigation;
using Warp9.Themes;
using Warp9.Utils;
using Warp9.Viewer;

namespace Warp9.ProjectExplorer
{
    public enum ProjectItemKind
    {
        Folder,
        Gallery,
        Viewer,
        Table,
        Other
    }

    public class ProjectItem
    {
        protected ProjectItem(Warp9ViewModel vm, Type? presenterType)
        {
            ParentViewModel = vm;
            PagePresenterType = presenterType;
        }

        public string Name { get; set; } = string.Empty;
        public ProjectItemKind Kind => GetKind();
        public ObservableCollection<ProjectItem> Children { get; set; } = new ObservableCollection<ProjectItem>();
        public Warp9ViewModel ParentViewModel { get; init; }
        public Type? PagePresenterType { get; init; }
        public bool IsNodeExpanded => true;
        public virtual void ConfigurePresenter(IWarp9View pres) { }

        public virtual void Update()
        {
            foreach (ProjectItem pi in Children)
                pi.Update();
        }

        protected virtual ProjectItemKind GetKind()
        {
            return ProjectItemKind.Other;
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

        protected override ProjectItemKind GetKind() => ProjectItemKind.Folder;
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

        protected override ProjectItemKind GetKind() => ProjectItemKind.Folder;
    }

    public class SpecimenTableProjectItem : ProjectItem
    {
        public SpecimenTableProjectItem(Warp9ViewModel vm, long key, string? explicitName = null) :
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

        protected override ProjectItemKind GetKind() => ProjectItemKind.Table;
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

            foreach (var kvp in ParentViewModel.Project.Entries)
            {
                switch (kvp.Value.Kind)
                {
                    case ProjectEntryKind.MeshCorrespondence:
                        Children.Add(new MeshCorrespondenceProjectItem(ParentViewModel, kvp.Key));
                        break;

                    case ProjectEntryKind.MeshPca:
                        Children.Add(new PcaProjectItem(ParentViewModel, kvp.Key));
                        break;
                }
            }

            base.Update();
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Folder;
    }

    public class MeshCorrespondenceProjectItem : ProjectItem
    {
        public MeshCorrespondenceProjectItem(Warp9ViewModel vm, long key) :
            base(vm, typeof(SummaryPage))
        {
            Key = key;
            Children.Add(new MeshCorrespondenceViewerProjectItem(vm, key, "DCA viewer"));
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

            if (pres is not SummaryPage page)
                throw new ArgumentException();

            if (ParentViewModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                page.Document = EntitySummary.SummarizeDca(ParentViewModel.Project, entry);
            else
                page.Document = new FlowDocument();
        }
    }

    public class MeshCorrespondenceViewerProjectItem : ProjectItem
    {
        public MeshCorrespondenceViewerProjectItem(Warp9ViewModel vm, long key, string name) :
            base(vm, typeof(ViewerPage))
        {
            Name = name;
            Key = key;
        }

        public long Key { get; init; }

        public override void ConfigurePresenter(IWarp9View pres)
        {
            base.ConfigurePresenter(pres);

            if (pres is not ViewerPage page)
                throw new ArgumentException();

            Project proj = ParentViewModel.Project;
            page.SetContent(
                new CorrMeshViewerContent(proj, Key, "Correspondence meshes"),
                new CompareGroupsViewerContent(proj, Key, "Compare groups"),
                new DcaDiagnosticsViewerContent(proj, Key, "DCA diagnostics"));
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Viewer;
    }

    public class PcaProjectItem : ProjectItem
    {
        public PcaProjectItem(Warp9ViewModel vm, long key) :
           base(vm, typeof(ViewerPage))
        {
            Key = key;
            Children.Add(new PcaTableProjectItem(vm, key));
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
                new PcaSynthMeshViewerContent(proj, Key, "Shape synthesis"));
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Viewer;
    }
    public class PcaTableProjectItem : ProjectItem
    {
        public PcaTableProjectItem(Warp9ViewModel vm, long key) :
            base(vm, typeof(MatrixViewPage))
        {
            Key = key;
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

            if (pres is not MatrixViewPage page)
                throw new ArgumentException();

            Project proj = ParentViewModel.Project;

            if (ParentViewModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) &&
                entry.Payload.PcaExtra is not null &&
                proj.TryGetReference(entry.Payload.PcaExtra.DataKey, out MatrixCollection? mat) &&
                mat is not null)
            {
                page.SetMatrices(new MatrixViewProvider(mat[1], "Variance", "Variance"),
                    (new MatrixViewProvider(mat[3], "Scores", "PC{0}")));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Table;
    }

    public class GalleryProjectItem : ProjectItem
    {
        public GalleryProjectItem(Warp9ViewModel vm) :
            base(vm, typeof(GalleryPage))
        {
            Name = "Gallery";
        }

        public override void ConfigurePresenter(IWarp9View pres)
        {
            if (pres is not GalleryPage page)
                throw new ArgumentException();

            page.UpdateGallery();
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Gallery;
    }
}