using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Warp9.Utils;

namespace Warp9.Forms
{
    /// <summary>
    /// Interaction logic for RenderSettingsWindow.xaml
    /// </summary>
    public partial class RenderSettingsWindow : Window
    {
        public RenderSettingsWindow()
        {
            InitializeComponent();
            DataContext = settings;
        }

        GalleryRenderSettings settings = new GalleryRenderSettings();

        public void AttachSettings(GalleryRenderSettings settings)
        {
            this.settings = settings;
            DataContext = settings;
        }

        private void DestinationBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = settings.Directory;
            if(dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                settings.Directory = dlg.SelectedPath;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
