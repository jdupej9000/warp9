using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Warp9.Avalonia.Navigation;
using Warp9.Avalonia.Utils;
using Warp9.Data;
using Warp9.Model;
using Warp9.Processing;
using static System.Windows.Forms.Design.AxImporter;

namespace Warp9.Avalonia
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
        protected ProjectItem(Warp9ProjectModel vm, Type? presenterType)
        {
            ParentModel = vm;
            PagePresenterType = presenterType;
        }

        public string Name { get; set; } = string.Empty;
        public string DisplayName
        {
            get
            {
                string? adv = GetAdvancedNamePart();
                if (adv is null || !Options.Instance.ShowProjectItemIds) return Name;
                else return string.Format("{0} ({1})", Name, adv);
            }
        }
        public ProjectItemKind Kind => GetKind();
        public ObservableCollection<ProjectItem> Children { get; set; } = new ObservableCollection<ProjectItem>();
        public Warp9ProjectModel ParentModel { get; init; }
        public Type? PagePresenterType { get; init; }
        public bool IsNodeExpanded => true;
        public virtual void ConfigurePresenter(object pres) { }

        protected virtual string? GetAdvancedNamePart()
        {
            return null;
        }

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

    #region General
    public class GeneralProjectItem : ProjectItem
    {
        public GeneralProjectItem(Warp9ProjectModel vm)
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
        public GeneralCommentProjectItem(Warp9ProjectModel vm) :
            base(vm, typeof(TextEditorPage))
        {
            Name = "Comment";
        }

        public override void ConfigurePresenter(object pres)
        {
            if (pres is not TextEditorPage page)
                throw new ArgumentException();
        }
    }

    public class GeneralSettingsProjectItem : ProjectItem
    {
        public GeneralSettingsProjectItem(Warp9ProjectModel vm) :
            base(vm, typeof(ProjectSettingsPage))
        {
            Name = "Settings";
        }

        public override void ConfigurePresenter(object pres)
        {
            if (pres is not ProjectSettingsPage page)
                throw new ArgumentException();
        }
    }
    #endregion

    #region Datasets
    public class DatasetsProjectItem : ProjectItem
    {
        public DatasetsProjectItem(Warp9ProjectModel vm) :
            base(vm, null)
        {
            Name = "Datasets";
            Update();
        }

        public override void Update()
        {
            List<SpecimenTableInfo> tables = ModelUtils.EnumerateSpecimenTables(ParentModel.Project).ToList();

            // TODO: do not remove items that were not changed
            Children.Clear();
            foreach (SpecimenTableInfo sti in tables)
                Children.Add(new SpecimenTableProjectItem(ParentModel, sti.SpecTableId));

            base.Update();
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Folder;
    }

    public class SpecimenTableProjectItem : ProjectItem
    {
        public SpecimenTableProjectItem(Warp9ProjectModel vm, long key, string? explicitName = null, bool fullResolve = false) :
            base(vm, typeof(SpecimenTablePage))
        {
            Key = key;
            ExplicitName = explicitName;
            FullResolve = fullResolve;
        }

        public long Key { get; init; }
        public string? ExplicitName { get; init; }
        public bool FullResolve { get; init; }

        protected override string? GetAdvancedNamePart()
        {
            return string.Format("#{0}", Key);
        }

        public override void ConfigurePresenter(object pres)
        {
            if (pres is not SpecimenTablePage page)
                throw new ArgumentException();

            page.ShowEntry(Key, FullResolve);
        }

        public override void Update()
        {
            if (ParentModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                Name = ExplicitName ?? entry.Name;
            else
                Name = "(error)";
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Table;
    }
    #endregion

    #region Results
    public class ResultsProjectItem : ProjectItem
    {
        public ResultsProjectItem(Warp9ProjectModel vm) :
            base(vm, null)
        {
            Name = "Results";
            Update();
        }

        public override void Update()
        {
            Children.Clear();

            foreach (var kvp in ParentModel.Project.Entries)
            {
                switch (kvp.Value.Kind)
                {
                    case ProjectEntryKind.MeshCorrespondence:
                        Children.Add(new MeshCorrespondenceProjectItem(ParentModel, kvp.Key));
                        break;

                    case ProjectEntryKind.MeshPca:
                        Children.Add(new PcaProjectItem(ParentModel, kvp.Key));
                        break;

                    case ProjectEntryKind.DiffMatrix:
                        Children.Add(new DiffMatrixProjectItem(ParentModel, kvp.Key));
                        break;
                }
            }

            base.Update();
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Folder;
    }

    public class MeshCorrespondenceProjectItem : ProjectItem
    {
        public MeshCorrespondenceProjectItem(Warp9ProjectModel vm, long key) :
            base(vm, typeof(SummaryPage))
        {
            Key = key;
            Children.Add(new MeshCorrespondenceViewerProjectItem(vm, key, "DCA viewer"));
            Children.Add(new SpecimenTableProjectItem(vm, key, "Results table", true));
        }

        public long Key { get; init; }

        protected override string? GetAdvancedNamePart()
        {
            return string.Format("#{0}", Key);
        }

        public override void Update()
        {
            if (ParentModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                Name = entry.Name;
            else
                Name = "(error)";

            base.Update();
        }

        public override void ConfigurePresenter(object pres)
        {
            base.ConfigurePresenter(pres);

            if (pres is not SummaryPage page)
                throw new ArgumentException();

            if (ParentModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                page.SetSummaryText(EntitySummary.SummarizeDca(ParentModel.Project, entry));
            else
                page.SetSummaryText(string.Empty);
        }
    }

    public class MeshCorrespondenceViewerProjectItem : ProjectItem
    {
        public MeshCorrespondenceViewerProjectItem(Warp9ProjectModel vm, long key, string name) :
            base(vm, typeof(ViewerPage))
        {
            Name = name;
            Key = key;
        }

        public long Key { get; init; }

        public override void ConfigurePresenter(object pres)
        {
            base.ConfigurePresenter(pres);

            if (pres is not ViewerPage page)
                throw new ArgumentException();
           /*
            Project proj = ParentViewModel.Project;
            page.SetContent(
                new CorrMeshViewerContent(proj, Key, "Correspondence meshes"),
                new CompareGroupsViewerContent(proj, Key, "Compare groups"),
                new RepeatedMeasurementsViewerContent(proj, Key, "Repeated measurements"),
                new DcaDiagnosticsViewerContent(proj, Key, "DCA diagnostics"));*/
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Viewer;
    }

    public class DiffMatrixProjectItem : ProjectItem
    {
        public DiffMatrixProjectItem(Warp9ProjectModel vm, long key) :
            base(vm, typeof(MainLandingPage))
        {
            Key = key;
        }

        public long Key { get; init; }

        protected override string? GetAdvancedNamePart()
        {
            return string.Format("#{0}", Key);
        }

        public override void Update()
        {
            if (ParentModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                Name = entry.Name;
            else
                Name = "(error)";

            base.Update();
        }

        public override void ConfigurePresenter(object pres)
        {
            base.ConfigurePresenter(pres);

            /*if (pres is not MatrixViewPage page)
                throw new ArgumentException();

            Project proj = ParentViewModel.Project;

            if (ParentViewModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) &&
                entry.Payload.DiffMatrixExtra is not null &&
                proj.TryGetReference(entry.Payload.DiffMatrixExtra.DataKey, out MatrixCollection? mat) &&
                mat is not null)
            {
                List<MatrixViewProvider> mats = new List<MatrixViewProvider>();
                foreach (var kvp in mat)
                {
                    MeshDistanceKind mdk = (MeshDistanceKind)kvp.Key;
                    mats.Add(new MatrixViewProvider(mat[kvp.Key], mdk.ToString()));
                }

                page.SetMatrices(mats.ToArray());
            }
            else
            {
                throw new InvalidOperationException();
            }*/
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Table;
    }

    public class PcaProjectItem : ProjectItem
    {
        public PcaProjectItem(Warp9ProjectModel vm, long key) :
           base(vm, typeof(MainLandingPage))
        {
            Key = key;
            Children.Add(new PcaTableProjectItem(vm, key));
        }

        public long Key { get; init; }

        protected override string? GetAdvancedNamePart()
        {
            return string.Format("#{0}", Key);
        }

        public override void Update()
        {
            if (ParentModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                Name = entry.Name;
            else
                Name = "(error)";

            base.Update();
        }

        public override void ConfigurePresenter(object pres)
        {
            base.ConfigurePresenter(pres);

           /* if (pres is not ViewerPage page)
                throw new ArgumentException();

            Project proj = ParentViewModel.Project;
            page.SetContent(
                new PcaSynthMeshViewerContent(proj, Key, "Shape synthesis"));*/
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Viewer;
    }

    public class PcaTableProjectItem : ProjectItem
    {
        public PcaTableProjectItem(Warp9ProjectModel vm, long key) :
            base(vm, typeof(MainLandingPage))
        {
            Key = key;
        }

        public long Key { get; init; }

        public override void Update()
        {
            if (ParentModel.Project.Entries.TryGetValue(Key, out ProjectEntry? entry) && entry is not null)
                Name = entry.Name;
            else
                Name = "(error)";

            base.Update();
        }

        public override void ConfigurePresenter(object pres)
        {
            base.ConfigurePresenter(pres);

            /*if (pres is not MatrixViewPage page)
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
            }*/
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Table;
    }
    #endregion

    #region Gallery
    public class GalleryProjectItem : ProjectItem
    {
        public GalleryProjectItem(Warp9ProjectModel vm) :
            base(vm, typeof(MainLandingPage))
        {
            Name = "Gallery";
        }

        public override void ConfigurePresenter(object pres)
        {
            /*if (pres is not GalleryPage page)
                throw new ArgumentException();

            page.UpdateGallery();*/
        }

        protected override ProjectItemKind GetKind() => ProjectItemKind.Gallery;
    }
    #endregion
}
