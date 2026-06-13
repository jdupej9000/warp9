using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Avalonia.VisualTree;
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

            page.AttachProject(ParentModel.Project);
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
}
