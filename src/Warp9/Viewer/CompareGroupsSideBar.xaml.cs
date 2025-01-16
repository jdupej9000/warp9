using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Warp9.Data;

namespace Warp9.Viewer
{
    public record PaletteItem
    {
        private PaletteItem(string name, BitmapSource bmp, (float, System.Drawing.Color)[] stops)
        {
            Name = name;
            Image = bmp;
            Stops = stops;
        }

        public string Name { get; }
        public BitmapSource Image { get; }

        public (float, System.Drawing.Color)[] Stops { get; }
        const int Width = 24, Height = 12;

        public static PaletteItem Create(string name, (float, System.Drawing.Color)[] stops)
        {
            Lut lut = Lut.Create(Width, stops);

            int[] pixels = new int[Width * Height];
            for (int i = 0; i < Width; i++)
            {
                int c = lut.Sample((float)i / (float)(Width - 1)).ToArgb();
                for (int j = 0; j < Height; j++)
                    pixels[j * Width + i] = c;
            }

            return new PaletteItem(name, BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgra32, null, pixels, Width * 4), stops);
        }
    }

    /// <summary>
    /// Interaction logic for CompareGroupsSideBar.xaml
    /// </summary>
    public partial class CompareGroupsSideBar : Page
    {
        public CompareGroupsSideBar(CompareGroupsViewerContent content)
        {
            InitializeComponent();
            Content = content;
            DataContext = content;
            cmbPalettes.ItemsSource = Palettes;
        }

        CompareGroupsViewerContent Content { get; init; }

        public readonly static List<PaletteItem> Palettes = new List<PaletteItem>
        {
            PaletteItem.Create("Fast", Lut.FastColors),
            PaletteItem.Create("Viridis", Lut.ViridisColors),
            PaletteItem.Create("Plasma", Lut.PlasmaColors),
            PaletteItem.Create("Black body", Lut.BlackBodyColors),
            PaletteItem.Create("Jet", Lut.JetColors),
            PaletteItem.Create("Blue to green", Lut.BlueToGreenColors),
            PaletteItem.Create("Shades of grey", Lut.GreyColors)
        };

        private void GroupA_Click(object sender, RoutedEventArgs e)
        {
            Content.InvokeGroupSelectionDialog(0);
        }

        private void GroupB_Click(object sender, RoutedEventArgs e)
        {
            Content.InvokeGroupSelectionDialog(1);
        }

        private void GroupSwap_Click(object sender, RoutedEventArgs e)
        {
            Content.SwapGroups();
        }

        public void SetHist(float[] values, Lut lut, float x0, float x1)
        {
            histField.SetAll(values, lut, x0, x1);
        }

        private void histField_ScaleHover(object sender, float? e)
        {
            Content.MeshScaleHover(e);
        }
    }
}
