using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Windows.Forms;
using Warp9.Model;
using static System.Windows.Forms.Design.AxImporter;

namespace Warp9.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Navigation.MainLandingPage pageLanding = new Navigation.MainLandingPage();
        Warp9ProjectModel? Model = null;

        private void HelpAbout_Click(object? sender, RoutedEventArgs e)
        {
            AboutWindow dlg = new AboutWindow();
            dlg.ShowDialog(this);
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            frameMain.Content = pageLanding;
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
    }
}