using SkiaSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Warp9.Data
{
    public enum PixelFormat
    {
        Gray8 = 0,
        Gray16,
        Rgbx8,
        Rgba8,
        Bgrx8,
        Bgra8,

        Unknown
    }

    public ref struct PixelFormatInfo
    {
        public PixelFormatInfo()
        {            
        }

        public int NumChannels;
        public int StructSize;
        public bool ChannelsAreFloat = false;

        public bool HasGrayChannel => GrayOffsetBit >= 0;
        public int GrayOffsetBit = -1;
        public int GrayWidthBit = 0;

        public bool HasAlphaChannel => AlphaOffsetBit >= 0;
        public int AlphaOffsetBit = -1;
        public int AlphaWidthBit = 0;

        public bool HasRedChannel => RedOffsetBit >= 0;
        public int RedOffsetBit = -1;
        public int RedWidthBit = 0;

        public bool HasGreenChannel => GreenOffsetBit >= 0;
        public int GreenOffsetBit = -1;
        public int GreenWidthBit = 0;

        public bool HasBlueChannel => BlueOffsetBit >= 0;
        public int BlueOffsetBit = -1;
        public int BlueWidthBit = 0;

        public static PixelFormatInfo MakeGray(int bits, bool fmtFloat)
        {
            return new PixelFormatInfo
            {
                NumChannels = 1,
                StructSize = bits / 8,
                ChannelsAreFloat = fmtFloat,
                GrayOffsetBit = 0,
                GrayWidthBit = bits
            };
        }

        public static PixelFormatInfo MakeRgbx(int bits, bool fmtFloat, bool bgr, bool alpha)
        {
            if(bgr)
            {
                return new PixelFormatInfo
                {
                    NumChannels = 4,
                    StructSize = 4 * bits / 8,
                    ChannelsAreFloat = fmtFloat,
                    BlueOffsetBit = 0, BlueWidthBit = bits,
                    GreenOffsetBit = bits, GreenWidthBit = bits,
                    RedOffsetBit = 2 * bits, RedWidthBit = bits,
                    AlphaOffsetBit = alpha ? 3 * bits : -1, AlphaWidthBit = alpha ? bits : 0
                };
            }
            else
            {
                return new PixelFormatInfo
                {
                    NumChannels = 4,
                    StructSize = 4 * bits / 8,
                    ChannelsAreFloat = fmtFloat,
                    RedOffsetBit = 0, RedWidthBit = bits,
                    GreenOffsetBit = bits, GreenWidthBit = bits,
                    BlueOffsetBit = 2 * bits, BlueWidthBit = bits,
                    AlphaOffsetBit = alpha ? 3 * bits : -1, AlphaWidthBit = alpha ? bits : 0
                };
            }
        }
    }

    public class RasterImage
    {
        public RasterImage(int h, int w, PixelFormat f)
        {
            height = h;
            width = w;
            fmt = f;
            PixelFormatInfo pfi = GetPixelFormatInfo(fmt);
            stride = w * pfi.StructSize;
            data = new byte[stride * height];
        }

         public RasterImage(int h, int w, int s, PixelFormat f)
        {
            height = h;
            width = w;
            fmt = f;            
            stride = s;
            data = new byte[stride * height];
        }

        byte[] data;
        readonly int width, height, stride;
        PixelFormat fmt;

        public int Width => width;
        public int Height => height;
        public int Stride => stride;
        public PixelFormat PixelFormat => fmt;

        public Span<byte> GetRawData()
        {
            return data.AsSpan();
        }

        public Span<T> GetRow<T>(int row) where T:unmanaged
        {
            PixelFormatInfo pfi = GetPixelFormatInfo(fmt);
            if(Marshal.SizeOf<T>() != pfi.StructSize)
                throw new InvalidOperationException();

            return MemoryMarshal.Cast<byte, T>(data.AsSpan(stride * row, stride));
        }

        public static RasterImage FromFile(string path)
        {
            using SKBitmap bmp = SKBitmap.Decode(path);

            RasterImage ri = new (bmp.Height, bmp.Width, bmp.RowBytes, MapFromSkiaFormat(bmp.ColorType));
            return ri;
        }

        public void Save(string path, int q=100)
        {
            SKEncodedImageFormat imageFormat = Path.GetExtension(path).ToLower() switch
            {
                ".png" => SKEncodedImageFormat.Png,
                ".jpg" or ".jpeg" or ".jpe" => SKEncodedImageFormat.Jpeg,
                _ => throw new NotSupportedException()
            };

            Save(imageFormat, q, (d) =>
            {
                using FileStream fs = File.OpenWrite(path);
                d.SaveTo(fs);
            });
        }

        private void Save(SKEncodedImageFormat imageFormat, int imageQ, Action<SKData> validDataProc)
        {
            SKImageInfo inf = new SKImageInfo(width, height, MapToSkiaFormat(fmt));
            using SKBitmap bmp = new SKBitmap(inf);
            GCHandle pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                bmp.InstallPixels(inf, pinned.AddrOfPinnedObject(), inf.RowBytes);
                using SKImage img = SKImage.FromBitmap(bmp);
                using SKData d = img.Encode(imageFormat, imageQ);
                validDataProc(d);
            }
            finally
            {
                pinned.Free();
            }
        }

        public static PixelFormatInfo GetPixelFormatInfo(PixelFormat fmt)
        {
            return fmt switch
            {
                PixelFormat.Gray8 => PixelFormatInfo.MakeGray(8, false),
                PixelFormat.Gray16 => PixelFormatInfo.MakeGray(16, false),
                PixelFormat.Rgbx8 => PixelFormatInfo.MakeRgbx(8, false, false, false),
                PixelFormat.Rgba8 => PixelFormatInfo.MakeRgbx(8, false, false, true),
                PixelFormat.Bgrx8 => PixelFormatInfo.MakeRgbx(8, false, true, false),
                PixelFormat.Bgra8 => PixelFormatInfo.MakeRgbx(8, false, true, true),
                _ => throw new NotSupportedException()
            };
        }

        private static PixelFormat MapFromSkiaFormat(SKColorType ct)
        {
            return ct switch
            {
                SKColorType.Rgba8888 => PixelFormat.Rgba8,
                SKColorType.Rgb888x => PixelFormat.Rgbx8,
                SKColorType.Bgra8888 => PixelFormat.Bgra8,
                SKColorType.Gray8 => PixelFormat.Gray8,               

                _ => throw new NotSupportedException($"Cannot interpret Skia color type '{ct}'.")
            };
        }

        private static SKColorType MapToSkiaFormat(PixelFormat pf)
        {
            return pf switch
            {
                PixelFormat.Gray8 => SKColorType.Gray8,
                //PixelFormat.Gray16 => PixelFormatInfo.MakeGray(16, false),
                PixelFormat.Rgbx8 => SKColorType.Rgb888x,
                PixelFormat.Rgba8 => SKColorType.Rgba8888,
                //PixelFormat.Bgrx8 => SKColorType.Rgb888x,
                PixelFormat.Bgra8 => SKColorType.Bgra8888,
                _ => throw new NotSupportedException()
            };
        }
    }
}