using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Warp9.Model;
using Warp9.ProjectExplorer;

namespace Warp9.Navigation
{
    public record GalleryItem(Project Project, SnapshotInfo Info)
    {
        public string Title => Info.Name;
        public BitmapSource? Thumbnail
        {
            get
            {
                if (Project.TryGetReference<System.Drawing.Bitmap>(Info.ThumbnailKey, out System.Drawing.Bitmap? bmp) &&
                    bmp is not null)
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }

                return null;
            }
        }
    };

    /// <summary>
    /// Interaction logic for GalleryPage.xaml
    /// </summary>
    public partial class GalleryPage : Page, IWarp9View
    {
        public GalleryPage()
        {
            InitializeComponent();
        }

        Warp9ViewModel? viewModel;

        public ObservableCollection<GalleryItem> GalleryItems { get; } = new ObservableCollection<GalleryItem>();

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
        }

        public void DetachViewModel()
        {
            viewModel = null;
        }

        public void UpdateGallery()
        {
            GalleryItems.Clear();

            if (viewModel is null)
                return;

            foreach (var kvp in viewModel.Project.Snapshots)
            {
                GalleryItems.Add(new GalleryItem(viewModel.Project, kvp.Value));
            }

            lstItems.ItemsSource = GalleryItems;
        }

        private void lstItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstItems.SelectedItems.Count == 1 &&
                e.AddedItems.Count == 1 &&
                e.AddedItems[0] is GalleryItem gi)
            {
                pnlItem.DataContext = gi;
            }
            else
            {
                pnlItem.DataContext = null;
            }

            e.Handled = true;
        }
    }
}
