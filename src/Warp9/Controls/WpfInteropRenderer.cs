using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interop;
using Warp9.Viewer;

namespace Warp9.Controls
{
    public class WpfInteropRenderer : RendererBase
    {
        private WpfInteropRenderer(SharpDX.DXGI.AdapterDescription desc, Device d)
        {
            device = d;
            ctx = d.ImmediateContext;
            deviceDesc = desc;
            stateCache = new StateCache(d);
        }

        RenderTargetView? renderTargetView;
        DepthStencilView? depthStencilView;
        Texture2D? texFboDepth;
        Size depthStencilLastSize = Size.Empty;

        public void Present()
        {
            if (device is null || ctx is null)
                throw new InvalidOperationException();

            if (depthStencilView is null || renderTargetView is null)
                throw new InvalidOperationException();

            ctx.ClearDepthStencilView(depthStencilView,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 255);
            ctx.ClearRenderTargetView(renderTargetView, RenderUtils.ToDxColor(CanvasColor));

            Render();

            ctx.Flush();

        }

        public void EnsureSharedBackBuffer(IntPtr resourcePtr, System.Windows.Size size)
        {
            if (device is null || ctx is null) 
                throw new InvalidOperationException();

            // convert native pointer to DXGI shared resource
            SharpDX.DXGI.Resource resource = CppObject.FromPointer<SharpDX.DXGI.Resource>(resourcePtr).QueryInterface<SharpDX.DXGI.Resource>();

            // convert shared resource to D3D11 Texture
            Texture2D sharedBackbuffer = device.OpenSharedResource<Texture2D>(resource.SharedHandle);

            // release reference
            resource.Dispose();

            // use D3D11 Texture as render target
            RenderTargetViewDescription desc = new RenderTargetViewDescription();
            desc.Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
            desc.Dimension = RenderTargetViewDimension.Texture2D;
            desc.Texture2D.MipSlice = 0;

            renderTargetView = new RenderTargetView(device, sharedBackbuffer, desc);

            // EnsureSharedBackBuffer can be called also on device change, but it is possible that window size
            // does not change. In that case we may have a stale depth buffer handle. This may not end well.
            EnsureDepthBuffer(new Size((int)size.Width, (int)size.Height));

            ctx.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);

            // release reference
            sharedBackbuffer.Dispose();

            ctx.Rasterizer.SetViewport(0, 0, (int)size.Width, (int)size.Height, 0.0f, 1.0f);
        }

        public static bool TryCreate(int adapter, out WpfInteropRenderer? ret)
        {
            using SharpDX.DXGI.Factory factory = new SharpDX.DXGI.Factory1();

            int adapterIdx = adapter;
            int numAdapters = factory.GetAdapterCount();
            if (numAdapters == 0)
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

                ret = new WpfInteropRenderer(a.Description, device);
                return true;
            }
            catch (Exception)
            {
                ret = null;
                return false;
            }
        }

        protected void EnsureDepthBuffer(Size size)
        {
            if (depthStencilLastSize != size)
            {
                if (depthStencilView is not null)
                    Utilities.Dispose(ref depthStencilView);

                if (texFboDepth is not null)
                    Utilities.Dispose(ref texFboDepth);

                const SharpDX.DXGI.Format fmtDepth = SharpDX.DXGI.Format.D24_UNorm_S8_UInt;

                texFboDepth = new Texture2D(device, new Texture2DDescription
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.DepthStencil,
                    Usage = ResourceUsage.Default,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = fmtDepth,
                    Height = size.Height,
                    Width = size.Width,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
                });

                depthStencilView = new DepthStencilView(device, texFboDepth, new DepthStencilViewDescription
                {
                    Dimension = DepthStencilViewDimension.Texture2D,
                    Format = fmtDepth
                });
            }
        }
    

    }
}
