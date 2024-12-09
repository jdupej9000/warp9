﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Warp9.Forms;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Navigation;
using Warp9.Processing;
using Warp9.ProjectExplorer;
using Warp9.Utils;
using Warp9.Viewer;

namespace Warp9
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            pageViewer = new ViewerPage(this);

            views.Add(pageViewer);
            views.Add(pageTextEditor);
            views.Add(pageSpecimenTable);
            views.Add(pageProjectSettings);
            views.Add(pageLog);

            lstTasks.ItemsSource = JobEngine.Jobs;
        }

        Warp9Model? model = null;

        MainLandingPage pageLanding = new MainLandingPage();
        LogPage pageLog = new LogPage();
        TextEditorPage pageTextEditor = new TextEditorPage();
        SpecimenTablePage pageSpecimenTable = new SpecimenTablePage();
        ProjectSettingsPage pageProjectSettings = new ProjectSettingsPage();
        JobEngine jobEngine = new JobEngine();
        List<IWarp9View> views = new List<IWarp9View>();
        ViewerPage pageViewer;

        public JobEngine JobEngine => jobEngine;

        
        private bool SaveOrSaveAs()
        {
            if (model is null)
                return true;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Warp9 Project Files (*.w9)|*.w9";

            DialogResult res = dlg.ShowDialog();
            if(res != System.Windows.Forms.DialogResult.OK) 
                return false;

            model.Save(dlg.FileName);

            return true;
        }

        private bool OfferSaveDirtyProject()
        {
            if (model is not null && model.IsDirty)
            {
                MessageBoxResult res = System.Windows.MessageBox.Show("Do you wish to save your changes?", "Warp9",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

                switch (res)
                {
                    case MessageBoxResult.Yes:
                        return SaveOrSaveAs();

                    case MessageBoxResult.No:
                        return true;

                    case MessageBoxResult.Cancel:
                        return false;
                }
            }

            return true;
        }

        private void mnuFileNew_Click(object sender, RoutedEventArgs e)
        {
            if (!OfferSaveDirtyProject())
                return;

           SetProject(Project.CreateEmpty());
        }

        private void mnuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!OfferSaveDirtyProject())
                return;

            FileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Warp9 Project Files (*.w9)|*.w9";

            DialogResult res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                Warp9ProjectArchive archive = new Warp9ProjectArchive(dlg.FileName, false);
                SetProject(Project.Load(archive));
            }
        }

        private void mnuFileSave_Click(object sender, RoutedEventArgs e)
        {
            SaveOrSaveAs();
        }

        private void mnuFileSaveAs_Click(object sender, RoutedEventArgs e)
        {
        }

        private void mnuFileClose_Click(object sender, RoutedEventArgs e)
        {
            if (!OfferSaveDirtyProject())
                return;

            UnsetProject();
        }

        private void mnuFileExit_Click(object sender, RoutedEventArgs e)
        {
            if (!OfferSaveDirtyProject())
                return;
        }

        private void mnuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow wnd = new AboutWindow();
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void mnuProjectAddNewSpecTable_Click(object sender, RoutedEventArgs e)
        {
            // TODO: wrap this
            if (model is not null)
            {
                ProjectEntry entry = model.Project.AddNewEntry(ProjectEntryKind.Specimens);
                entry.Name = "Specimens";
                entry.Payload.Table = new SpecimenTable();
                SpecimenTableColumn<string> colName = entry.Payload.Table.AddColumn<string>("name", SpecimenTableColumnType.String);
                SpecimenTableColumn<int> colSex = entry.Payload.Table.AddColumn<int>("sex", SpecimenTableColumnType.Factor, ["F", "M"]);
                SpecimenTableColumn<bool> colStarfleet = entry.Payload.Table.AddColumn<bool>("starfleet", SpecimenTableColumnType.Boolean);
                SpecimenTableColumn<double> colHeight = entry.Payload.Table.AddColumn<double>("height", SpecimenTableColumnType.Real);
                colName.Data.AddRange(["Benjamin Sisko", "Kira Nerys", "Miles O'Brien", "Odo", "Jadzia Dax", "Weyoun"]);
                colSex.Data.AddRange([1, 0, 1, 1, 0, 1]);
                colStarfleet.Data.AddRange([true, false, true, false, true, false]);
                colHeight.Data.AddRange([1.85, 1.72, 1.80, 1.83, 1.82, 1.70]);

                model.ViewModel.Update();
            }
        }

        private void mnuProjectImportSpecTable_Click(object sender, RoutedEventArgs e)
        {
            if (model is null) throw new InvalidOperationException();

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Comma separated values (*.csv)|*.csv";

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            CsvImporter importer = CsvImporter.Create(dlg.FileName);

            ImportCsvWindow importDlg = new ImportCsvWindow();
            importDlg.AttachImporter(importer);
            importDlg.ShowDialog();
            if (importDlg.DialogResult != true) return;

            SpecimenTableImportWindow assignDlg = new SpecimenTableImportWindow();
            assignDlg.AttachImporter(importer);
            assignDlg.ShowDialog();
            if (assignDlg.DialogResult != true) return;

            SpecimenTable specTab = SpecimenTableGenerator.FromImporter(importer, assignDlg.ImportOperations, model.Project);
            ProjectEntry entry = model.Project.AddNewEntry(ProjectEntryKind.Specimens);
            entry.Name = "Imported";
            entry.Payload.Table = specTab;
            
            model.ViewModel.Update();
        }

        private void SetProject(Project project)
        {
            model = new Warp9Model(project);
            UpdateProjectExplorer();

            foreach (IWarp9View view in views)
                view.AttachViewModel(model.ViewModel);
        }

        private void UnsetProject()
        {
            if (model is not null)
            {
                model = null;
                UpdateProjectExplorer();

                foreach (IWarp9View view in views)
                    view.DetachViewModel();
            }
        }

        private void UpdateProjectExplorer()
        {
            treeProject.ItemsSource = model?.ViewModel?.Items ?? new ObservableCollection<ProjectItem>();
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void treeProject_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (model is not null && e.NewValue is ProjectItem item)
            {
                switch (item.Kind)
                {
                    case StockProjectItemKind.GeneralComment:
                        frameMain.NavigationService.Navigate(pageTextEditor);
                        break;

                    case StockProjectItemKind.GeneralSettings:
                        frameMain.NavigationService.Navigate(pageProjectSettings);
                        break;

                    case StockProjectItemKind.Entry:
                        if (model.Project.Entries.TryGetValue(item.EntryIndex, out ProjectEntry? entry))
                        {
                            switch (entry.Kind)
                            {
                                case ProjectEntryKind.Specimens:
                                    frameMain.NavigationService.Navigate(pageSpecimenTable);
                                    pageSpecimenTable.ShowEntry(item.EntryIndex);
                                    break;

                                case ProjectEntryKind.MeshCorrespondence:
                                    frameMain.NavigationService.Navigate(pageViewer);
                                    pageViewer.SetContent(new DcaViewerContent(model.Project, item.EntryIndex, "Correspondence meshes"));
                                    break;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        break;

                    default:
                        frameMain.NavigationService.Navigate(pageLog);
                        break;
                }
            }
            else
            {
                frameMain.NavigationService.Navigate(pageLog);
            }

            e.Handled = true;
        }

        private void mnuProjectComputeDca_Click(object sender, RoutedEventArgs e)
        {
            if (model is null)
                throw new InvalidOperationException();

            DcaConfiguration config = new DcaConfiguration();

            DcaConfigWindow cfgWnd = new DcaConfigWindow();
            cfgWnd.Attach(model.Project, config);
            cfgWnd.ShowDialog();

            if (cfgWnd.DialogResult is null || cfgWnd.DialogResult == false)
                return;

            ProjectJobContext ctx = new ProjectJobContext(model.Project);
            ctx.LogMessage += (s,e) => pageLog.AddMessage(e);
            Job job = Job.Create(DcaJob.Create(config, model.Project), ctx, "DCA");

            frameMain.NavigationService.Navigate(pageLog);
            JobEngine.Run(job);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            jobEngine.Dispose();
        }
    }
}