using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Warp9.Avalonia.Navigation;
using Warp9.Model;
using static System.Windows.Forms.Design.AxImporter;

namespace Warp9.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            pages.Add(typeof(MainLandingPage), new MainLandingPage());
            pages.Add(typeof(TextEditorPage), new TextEditorPage());
            pages.Add(typeof(ProjectSettingsPage), new ProjectSettingsPage());
            pages.Add(typeof(SpecimenTablePage), new SpecimenTablePage());
            pages.Add(typeof(SummaryPage), new SummaryPage());
        }

        Dictionary<Type, ContentPage> pages = new Dictionary<Type, ContentPage>();
        Warp9ProjectModel? Model = null;
        ContentPage? ActivePage = null;

        private void SetPage(Type? pageType, ProjectItem? pi = null)
        {
            if (pageType is not null &&
                pages.TryGetValue(pageType, out ContentPage? page) && page is not null)
            {
                if (ActivePage is not null &&
                    ActivePage != page &&
                    ActivePage is IWarp9View oldView)
                {
                    oldView.DetachViewModel();
                }

                if (page is IWarp9View pageView && Model is not null)
                    pageView.AttachViewModel(Model);

                pi?.ConfigurePresenter(page);
                ActivePage = page;
            }
            else
            {
                if (ActivePage is not null &&
                    ActivePage is IWarp9View oldView)
                {
                    oldView.DetachViewModel();
                }

                ActivePage = pages[typeof(MainLandingPage)];
            }

            frameMain.Content = ActivePage;
        }

        private void HelpAbout_Click(object? sender, RoutedEventArgs e)
        {
            AboutWindow dlg = new AboutWindow();
            dlg.ShowDialog(this);
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            SetPage(null);
        }

        private void FileOpen_Click(object? sender, RoutedEventArgs e)
        {
            FileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Warp9 Project Files (*.w9)|*.w9";

            DialogResult res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    Warp9ProjectArchive archive = new Warp9ProjectArchive(dlg.FileName, false, true);// Options.Instance.NumWorkerThreads > 1)
                    Project proj = Project.Load(archive);
                    Model = new Warp9ProjectModel(proj);
                    DataContext = Model;
                }
                catch (Exception ex)
                {
                    //System.Windows.MessageBox.Show("Failed to load project: " + ex.Message, "Warp9 - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void trvProject_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && 
                e.AddedItems[0] is ProjectItem pi)
            {
                SetPage(pi.PagePresenterType, pi); 
            }
        }
    }
}