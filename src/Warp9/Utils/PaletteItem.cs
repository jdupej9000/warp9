using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Warp9.Data;

namespace Warp9.Utils
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

        public static readonly List<PaletteItem> KnownPaletteItems = new List<PaletteItem>
        {
            PaletteItem.Create("Fast", Lut.FastColors),
            PaletteItem.Create("Viridis", Lut.ViridisColors),
            PaletteItem.Create("Plasma", Lut.PlasmaColors),
            PaletteItem.Create("Black body", Lut.BlackBodyColors),
            PaletteItem.Create("Jet", Lut.JetColors),
            PaletteItem.Create("Blue to green", Lut.BlueToGreenColors),
            PaletteItem.Create("Shades of grey", Lut.GreyColors)
        };
    }
}
