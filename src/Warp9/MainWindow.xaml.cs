using SharpDX.Direct3D11;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Warp9.Forms;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Navigation;
using Warp9.Processing;
using Warp9.ProjectExplorer;
using Warp9.Themes;
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

            views.Add(typeof(ViewerPage), pageViewer);
            views.Add(typeof(TextEditorPage), pageTextEditor);
            views.Add(typeof(SpecimenTablePage), pageSpecimenTable);
            views.Add(typeof(ProjectSettingsPage), pageProjectSettings);
            views.Add(typeof(LogPage), pageLog);
            views.Add(typeof(MatrixViewPage), pageMatrixView);
            views.Add(typeof(SummaryPage), pageSummary);
            views.Add(typeof(GalleryPage), pageGallery);
        }

        Warp9Model? model = null;

        readonly LogPage pageLog = new LogPage();
        readonly SummaryPage pageSummary = new SummaryPage();
        readonly TextEditorPage pageTextEditor = new TextEditorPage();
        readonly SpecimenTablePage pageSpecimenTable = new SpecimenTablePage();
        readonly ProjectSettingsPage pageProjectSettings = new ProjectSettingsPage();
        readonly MatrixViewPage pageMatrixView = new MatrixViewPage();
        readonly GalleryPage pageGallery = new GalleryPage();
        readonly Dictionary<Type, IWarp9View> views = new Dictionary<Type, IWarp9View>();
        readonly ViewerPage pageViewer;
        
        private bool OfferSaveDirtyProject()
        {
            if (model is not null && model.IsDirty)
            {
                MessageBoxResult res = System.Windows.MessageBox.Show("Do you wish to save your changes?", "Warp9",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

                switch (res)
                {
                    case MessageBoxResult.Yes:
                        model?.ViewModel?.Save();
                        return true;

                    case MessageBoxResult.No:
                        return true;

                    case MessageBoxResult.Cancel:
                        return false;
                }
            }

            return true;
        }

        private void JobEngine_ProgressChanged(object? sender, JobEngineProgress e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
            { 
                if (e.IsBusy)
                {
                    if(e.NumJobsQueued > 0)
                        lblStatusMain.Text = $"({e.NumItemsDone} of {e.NumItems} done) {e.CurrentItemText} +{e.NumJobsQueued} queued jobs";
                    else
                        lblStatusMain.Text = $"({e.NumItemsDone} of {e.NumItems} done) {e.CurrentItemText}";

                    prbStatusProgress.Visibility = Visibility.Visible;
                    prbStatusProgress.Value = e.NumItemsDone;
                    prbStatusProgress.Maximum = e.NumItems;
                }
                else
                {
                    lblStatusMain.Text = "Ready.";
                    prbStatusProgress.Visibility = Visibility.Hidden;
                    WindowsSleepPrevention.AllowSleep();

                    UpdateProjectExplorer();
                }
            }));
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
                try
                {
                    Warp9ProjectArchive archive = new Warp9ProjectArchive(dlg.FileName, false, Options.Instance.NumWorkerThreads > 1);
                    SetProject(Project.Load(archive));
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Failed to load project: " + ex.Message, "Warp9 - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void mnuFileSave_Click(object sender, RoutedEventArgs e)
        {
            model?.ViewModel?.Save();
        }

        private void mnuFileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            model?.ViewModel?.Save(true);
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
            model?.ViewModel?.ImportSpecTable();
        }

        private void SetProject(Project project)
        {
            model?.Dispose();

            try
            {
                model = new Warp9Model(project, Options.Instance.NumWorkerThreads);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to set project: " + ex.Message, "Warp9 - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            model.JobEngine.ProgressChanged += JobEngine_ProgressChanged;
            model.LogMessage += (s, e) => pageLog.AddMessage(e);
            model.ModelEvent += Model_ModelEvent;
            DataContext = model.ViewModel;
            UpdateProjectExplorer();

            foreach (IWarp9View view in views.Values)
                view.AttachViewModel(model.ViewModel);
        }

        private void Model_ModelEvent(object? sender, ModelEventInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.Kind)
                {
                    case ModelEventKind.JobStarting:
                        frameMain.NavigationService.Navigate(pageLog);
                        if (Options.Instance.PreventSleepWhenBusy)
                            WindowsSleepPrevention.PreventSleep();

                        break;

                    case ModelEventKind.ProjectSaved:
                        lblStatusMain.Text = $"Saved as '{e.FileName}'.";
                        prbStatusProgress.Visibility = Visibility.Hidden;
                        break;
                }
            });
        }

        private void UnsetProject()
        {
            if (model is not null)
            {
                model.Dispose();
                model = null;
                DataContext = null;
                UpdateProjectExplorer();

                foreach (IWarp9View view in views.Values)
                    view.DetachViewModel();
            }
        }

        private void UpdateProjectExplorer()
        {
            model?.ViewModel?.Update();
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void treeProject_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (model is not null &&
                e.NewValue is ProjectItem item &&
                item.PagePresenterType is not null &&
                views.TryGetValue(item.PagePresenterType, out IWarp9View? viewer) &&
                viewer is not null)
            {
                frameMain.NavigationService.Navigate(viewer);
                item.ConfigurePresenter(viewer);
            }
            
            e.Handled = true;
        }

        private void mnuProjectComputeDca_Click(object sender, RoutedEventArgs e)
        {
            model?.ViewModel?.ComputeDca();
        }

        private void mnuProjectComputePca_Click(object sender, RoutedEventArgs e)
        {
            model?.ViewModel?.ComputeDcaPca();
        }

        private void mnuProjectComputeLmDiag_Click(object sender, RoutedEventArgs e)
        {
            model?.ViewModel?.ComputeLandmarkDiag();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            UnsetProject();
            Options.Save();
        }

        private void mnuToolsOptions_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow wnd = new OptionsWindow();
            wnd.ShowDialog();
        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {
            frameMain.NavigationService.Navigate(pageLog);
        }
    }
}