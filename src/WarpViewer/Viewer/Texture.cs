﻿using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using Warp9.Data;

namespace Warp9.Viewer
{
    public class Texture : IDisposable
    {
        private Texture(Texture1D tex, ShaderResourceView srv, Texture1DDescription desc)
        {
            texture = tex;
            resourceView = srv;

            Dimension = 1;
            Width = desc.Width;
            Format = desc.Format;
            IsDynamic = (desc.Usage == ResourceUsage.Dynamic);
        }

        private Texture(Texture2D tex, ShaderResourceView srv, Texture2DDescription desc)
        {
            texture = tex;
            resourceView = srv;

            Dimension = 2;
            Width = desc.Width;
            Height = desc.Height;
            Format = desc.Format;
            IsDynamic = (desc.Usage == ResourceUsage.Dynamic);
        }

        Resource texture;
        readonly ShaderResourceView resourceView;

        public int Dimension {get; private set;}
        public int Width {get; private set; }
        public int Height { get; private set; } = 0;
        public int Depth { get; private set; } = 0;
        public bool IsDynamic { get; private set; } = false;
        public SharpDX.DXGI.Format Format { get; private set; }
        public ShaderResourceView ResourceView => resourceView;

        public bool TryUpdateDynamic(DeviceContext ctx, Bitmap bitmap)
        {
            return false;
        }

        public bool TryUpdateDynamic(DeviceContext ctx, Lut lut)
        {
            return false;
        }

        public void Dispose()
        {
            Utilities.Dispose(ref texture);
            resourceView.Dispose();
        }

        internal static Texture Create(Device device, Lut lut, bool dynamic=false)
        {
            Texture1DDescription desc = new Texture1DDescription()
            {
                Width = lut.NumPixels,
                MipLevels = 1,
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                Usage = dynamic ? ResourceUsage.Dynamic : ResourceUsage.Default,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm
            };

            using DataStream ds = new DataStream(4 * lut.NumPixels, true, true);
            ds.Write(lut.Data, 0, lut.NumPixels * 4);
            ds.Position = 0;
            Texture1D tex = new Texture1D(device, desc, ds);

            ShaderResourceView srv = new ShaderResourceView(device, tex);

            return new Texture(tex, srv, desc);
        }

        internal static Texture Create(Device device, Bitmap bitmap, bool dynamic=false)
        {
            SharpDX.DXGI.Format dxgiFmt = SharpDX.DXGI.Format.Unknown;
            Bitmap bmp = bitmap;
            bool destroy = false;
            switch (bmp.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    bmp = new Bitmap(bmp);
                    destroy = true;
                    bmp.ConvertFormat(PixelFormat.Format32bppRgb);
                    dxgiFmt = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
                    break;

                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppRgb:
                    dxgiFmt = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
                    break;

                case PixelFormat.Format8bppIndexed:
                    dxgiFmt = SharpDX.DXGI.Format.R8_UNorm;
                    break;

                case PixelFormat.Format16bppGrayScale:
                    dxgiFmt = SharpDX.DXGI.Format.R16_UNorm;
                    break;

                default:
                    throw new ArgumentException("Bitmap pixel format is not supported.");
            }

            Texture2DDescription desc = new Texture2DDescription()
            {
                Width = bitmap.Width,
                Height = bitmap.Height,
                MipLevels = 1,
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = dynamic ? ResourceUsage.Dynamic : ResourceUsage.Default,
                Format = dxgiFmt
            };

            BitmapData bitmapData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite, bmp.PixelFormat);

            DataRectangle rect = new DataRectangle(bitmapData.Scan0, bitmapData.Stride);
            Texture2D tex = new Texture2D(device, desc, rect);

            bmp.UnlockBits(bitmapData);

            if (destroy)
                bmp.Dispose();

            ShaderResourceView srv = new ShaderResourceView(device, tex);
            return new Texture(tex, srv, desc);
        }
    }
}
