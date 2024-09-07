using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Viewer
{
    public class Texture : IDisposable
    {
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

        public void Dispose()
        {
            Utilities.Dispose(ref texture);
        }

        internal static Texture Create(Device dev, Lut lut)
        {
            throw new NotImplementedException();
        }

        internal static Texture Create(Device device, Bitmap bitmap, bool dynamic=false)
        {
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
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm
            };

            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, bitmap.PixelFormat);

            DataRectangle rect = new DataRectangle(bitmapData.Scan0, bitmapData.Stride);
            Texture2D tex = new Texture2D(device, desc, rect);

            bitmap.UnlockBits(bitmapData);

            ShaderResourceView srv = new ShaderResourceView(device, tex);
            return new Texture(tex, srv, desc);
        }
    }
}
