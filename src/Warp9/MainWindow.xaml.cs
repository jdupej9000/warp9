using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;
using Warp9.Controls;
using Warp9.Data;
using Warp9.IO;
using Warp9.Model;
using Warp9.Navigation;
using Warp9.ProjectExplorer;
using Warp9.Themes;
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
        }


       

        Warp9Model? model = null;

        MainLandingPage pageLanding = new MainLandingPage();
        TextEditorPage pageTextEditor = new TextEditorPage();
        SpecimenTablePage pageSpecimenTable = new SpecimenTablePage();
        List<IWarp9View> views = new List<IWarp9View>();
        ViewerPage pageViewer;

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Size size = WpfSizeToPixels(ImageGrid);
            //InteropImage.SetPixelSize((int)size.Width, (int)size.Height);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //Grid_Loaded2(sender, e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
           // CompositionTarget.Rendering -= CompositionTarget_Rendering;
            // TODO: renderer dispose
        }

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
                entry.Payload.Table.AddColumn<long>("id", SpecimenTableColumnType.Integer);
                entry.Payload.Table.AddColumn<string>("name", SpecimenTableColumnType.String);
                model.ViewModel.Update();
            }
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

                    case StockProjectItemKind.Entry:
                        if (model.Project.Entries.TryGetValue(item.EntryIndex, out ProjectEntry? entry))
                        {
                            switch (entry.Kind)
                            {
                                case ProjectEntryKind.Specimens:
                                    frameMain.NavigationService.Navigate(pageSpecimenTable);
                                    pageSpecimenTable.ShowEntry(item.EntryIndex);
                                    break;

                            }
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        break;

                    default:
                        frameMain.NavigationService.Navigate(pageViewer);
                        break;
                }
            }
            else
            {
                frameMain.NavigationService.Navigate(pageLanding);
            }

            e.Handled = true;
        }

      
    }
}