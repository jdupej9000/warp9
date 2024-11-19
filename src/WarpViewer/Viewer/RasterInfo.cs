using System;

namespace Warp9.Viewer
{
    public enum ChannelFormat
    {
        Invalid,
        Gray8,
        Bgra8
    }


    public struct RasterInfo
    {
        public RasterInfo(int width, int height, ChannelFormat fmt = ChannelFormat.Bgra8)
        {
            Width = width;
            Height = height;
            Format = fmt;
        }

        public int Width, Height;
        public ChannelFormat Format;

        public int SizeBytes => Width * Height * BytesPerPixel;

        public int BytesPerPixel
        {
            get
            {
                return Format switch
                {
                    ChannelFormat.Gray8 => 1,
                    ChannelFormat.Bgra8 => 4,
                    _ => throw new InvalidOperationException()
                };
            }
        }

        public System.Drawing.Imaging.PixelFormat PixelFormat
        {
            get
            {
                return Format switch
                {
                    ChannelFormat.Gray8 => System.Drawing.Imaging.PixelFormat.Format8bppIndexed,
                    ChannelFormat.Bgra8 => System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                    _ => throw new NotImplementedException()
                };
            }
        }

       
    }
}
