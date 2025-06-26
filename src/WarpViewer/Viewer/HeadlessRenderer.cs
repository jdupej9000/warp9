using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Drawing;
using System.Threading;

namespace Warp9.Viewer
{
    public class HeadlessRenderer : RendererBase
    {
        private HeadlessRenderer(SharpDX.DXGI.AdapterDescription desc, Device d)
        {
            device = d;
            ctx = d.ImmediateContext;
            deviceDesc = desc;
            stateCache = new StateCache(d);
        }

        RenderTargetView? renderTargetView;
        DepthStencilView? depthStencilView;
        Texture2D? texFboColor, texFboDepth, texStagingColor;
        Query? renderDoneQuery;

        bool rasterInfoDirty;
        RasterInfo rasterInfoCurrent, rasterInfoNew;

        public RasterInfo RasterFormat
        { 
            get { return rasterInfoCurrent; }
            set { rasterInfoNew = value; rasterInfoDirty = true; }
        }

        public void Present()
        {
            if (device is null || ctx is null)
                throw new InvalidOperationException();

            if (rasterInfoDirty)
            {
                CreateFboStaging();
                ctx.Rasterizer.SetViewport(0, 0, rasterInfoNew.Width, rasterInfoNew.Height, 0, 1);
                rasterInfoCurrent = rasterInfoNew;
                rasterInfoDirty = false;
            }

            if (renderTargetView is null || depthStencilView is null)
                throw new InvalidOperationException();

            ctx.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);

            PrepareRenderDone();

            ctx.ClearDepthStencilView(depthStencilView,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 255);
            ctx.ClearRenderTargetView(renderTargetView, RenderUtils.ToDxColor(CanvasColor));

            Render();

            ctx.CopyResource(texFboColor, texStagingColor);
            ctx.Flush();
            ctx.End(renderDoneQuery);

            WaitForRenderDone();
        
        }
        public void ExtractColor(Span<byte> result)
        {
            if (ctx is null || texStagingColor is null)
                throw new InvalidOperationException();

            int resultSize = rasterInfoCurrent.SizeBytes;
            ctx.MapSubresource(texStagingColor, 0, 0, MapMode.Read, MapFlags.None, out DataStream stream);
            stream.Read(result);
            ctx.UnmapSubresource(texStagingColor, 0);
            Utilities.Dispose(ref stream);
        }

        public Bitmap ExtractColorAsBitmap()
        {
            Bitmap ret = new Bitmap(rasterInfoCurrent.Width, rasterInfoCurrent.Height, rasterInfoCurrent.PixelFormat);
            System.Drawing.Imaging.BitmapData data = ret.LockBits(
                new Rectangle(0, 0, rasterInfoCurrent.Width, rasterInfoCurrent.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite, rasterInfoCurrent.PixelFormat);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                ExtractColor(new Span<byte>(ptr, rasterInfoCurrent.SizeBytes));
            }

            ret.UnlockBits(data);

            return ret;
        }

        protected override Size GetViewportSize()
        {
            return new Size(rasterInfoCurrent.Width, rasterInfoCurrent.Height);
        }

        protected void PrepareRenderDone()
        {
            if (device is null) return;

            QueryDescription renderDoneQueryDesc = new QueryDescription
            {
                Flags = QueryFlags.None,
                Type = QueryType.Event
            };

            renderDoneQuery = new Query(device, renderDoneQueryDesc);
        }

        protected void WaitForRenderDone()
        {
            if (ctx is null) return;

            uint queryRes = 0;
            while (!ctx.GetData(renderDoneQuery, AsynchronousFlags.DoNotFlush, out queryRes) &&
                queryRes != 0)
            {
                Thread.Yield();
            }

            Utilities.Dispose(ref renderDoneQuery);
        }

        protected void CreateFboStaging()
        {
            if (device is null) return;

            DestroyFboStaging();

            SharpDX.DXGI.Format fmtColor = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
            SharpDX.DXGI.Format fmtDepth = SharpDX.DXGI.Format.D24_UNorm_S8_UInt;
            int w = rasterInfoNew.Width;
            int h = rasterInfoNew.Height;

            texFboColor = new Texture2D(device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = fmtColor,
                Height = h,
                Width = w,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            });

            texFboDepth = new Texture2D(device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = fmtDepth,
                Height = h,
                Width = w,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            });

            texStagingColor = new Texture2D(device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.None,
                Usage = ResourceUsage.Staging,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = fmtColor,
                Height = h,
                Width = w,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            });

            renderTargetView = new RenderTargetView(device, texFboColor);
            depthStencilView = new DepthStencilView(device, texFboDepth, new DepthStencilViewDescription
            {
                Dimension = DepthStencilViewDimension.Texture2D,
                Format = fmtDepth
            });
        }

        protected void DestroyFboStaging()
        {
            if (depthStencilView is not null)
                Utilities.Dispose(ref depthStencilView);

            if(renderTargetView is not null)
                Utilities.Dispose(ref renderTargetView);
        
            if(texFboColor is not null)
                Utilities.Dispose(ref texFboColor);

            if(texFboDepth is not null)
                Utilities.Dispose(ref texFboDepth);

            if(texStagingColor is not null)
                Utilities.Dispose(ref texStagingColor);
        }

        public static bool TryCreate(int adapter, out HeadlessRenderer? ret)
        {
            using SharpDX.DXGI.Factory factory = new SharpDX.DXGI.Factory1();

            int adapterIdx = adapter;
            int numAdapters = factory.GetAdapterCount();
            if(numAdapters == 0)
            {
                ret = null;
                return false;
            }

            if (adapterIdx >= numAdapters)
                adapterIdx = 0;

            SharpDX.DXGI.Adapter a = factory.GetAdapter(adapterIdx);

            try
            {
                Device device = new Device(a, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug, 
                    SharpDX.Direct3D.FeatureLevel.Level_11_0);

                ret = new HeadlessRenderer(a.Description, device);
                return true;
            }
            catch (Exception)
            {
                ret = null;
                return false;
            }
        }

    }
}
