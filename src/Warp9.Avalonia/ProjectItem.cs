using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Warp9.Model;
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
}
