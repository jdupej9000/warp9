using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;

namespace Warp9.Viewer
{
    public record PresentingInfo(Size ViewportSize, long FrameIdx);

    public interface IRendererViewport
    {
        public Color CanvasColor { get; }
        public Size ViewportSize { get; }
        public long FrameIdx { get; }
    };

    public abstract class RendererBase : IRendererViewport
    {
        public RendererBase()
        {
        }

        protected Device? device;
        protected DeviceContext? ctx;
        protected SharpDX.DXGI.AdapterDescription deviceDesc;
        protected ShaderRegistry shaders = new ShaderRegistry();
        protected ConstantBufferManager constantBufferManager = new ConstantBufferManager();
        protected StateCache? stateCache;
        Dictionary<int, ConstantBufferPayload> constantBuffers = new Dictionary<int, ConstantBufferPayload>();
        readonly Dictionary<RenderItemBase, RenderJob?> renderItems = new Dictionary<RenderItemBase, RenderJob?>();
        protected bool jobsDirty = false;
        protected long frameIdx = 0;

        public ShaderRegistry Shaders => shaders;
        public string DeviceName => deviceDesc.Description;
        public Color CanvasColor { get; set; }
        public Size ViewportSize => GetViewportSize();
        public long FrameIdx => frameIdx;

        public event EventHandler<PresentingInfo>? Presenting;

        public void AddRenderItem(RenderItemBase renderItem)
        {
            lock (renderItems)
            {
                renderItem.Version.Commit(RenderItemDelta.Full);
                renderItems.Add(renderItem, null);
                jobsDirty = true;
            }
        }

        public void ClearRenderItems()
        {
            lock (renderItems)
            {
                renderItems.Clear();
                jobsDirty = true;
            }
        }

        protected void Destroy()
        {
            shaders.Dispose();
            constantBufferManager.Dispose();
            stateCache?.Dispose();

            foreach (var kvp in renderItems)
                kvp.Value?.Dispose();

            Utilities.Dispose(ref device);
        }

        protected void Render()
        {
            if (device is null || ctx is null || stateCache is null)
                throw new InvalidOperationException();

            PresentingInfo info = new PresentingInfo(
                GetViewportSize(),
                frameIdx);

            Presenting?.Invoke(this, info);

            List<(RenderItemBase, RenderJob?)> updates = new List<(RenderItemBase, RenderJob?)>();
            lock (renderItems)
            {
                foreach (var kvp in renderItems)
                {
                    RenderItemBase ri = kvp.Key;
                    RenderJob? job = kvp.Value;

                    RenderItemDelta delta = ri.UpdateRenderJob(ref job, ctx, shaders, constantBufferManager);
                    if(delta == RenderItemDelta.Full)
                        updates.Add((ri, job));
                }

                jobsDirty = false;

                foreach (var update in updates)
                    renderItems[update.Item1] = update.Item2;
                                
                // force setting the rasterizer state at least once
                stateCache.ResetLastState();

                List<KeyValuePair<RenderItemBase, RenderJob?>> sorted = new List<KeyValuePair<RenderItemBase, RenderJob?>>();
                sorted.AddRange(renderItems);
                sorted.Sort((a,b) => a.Key.Order.CompareTo(b.Key.Order));

                foreach (var kvp in sorted)
                {
                    if (kvp.Value is not null)
                    {
                        ResetState();

                        // Restore global constant buffers.
                        foreach (var cb in constantBuffers)
                            constantBufferManager.Set(ctx, cb.Key, cb.Value);

                        // Update individual or per-drawcall vertex buffers.
                        kvp.Key.UpdateConstantBuffers(kvp.Value, this);
                        RenderJobExecuteStatus renderStatus = kvp.Value.Render(ctx, stateCache);
                       
                        if (renderStatus != RenderJobExecuteStatus.Ok)
                        {

#if DEBUG
    if(renderStatus != RenderJobExecuteStatus.Ok)
        throw new InvalidOperationException($"Render job of {kvp.Key.ToString()} failed with {renderStatus}.");
#else
                            Console.Error.WriteLine($"Render job of {kvp.Key} failed with {renderStatus}.");
#endif
                        }
                    }
                }

                frameIdx++;
            }
        }

        protected void ResetState()
        {           
        }

        protected abstract Size GetViewportSize();

        public void SetConstant<T>(int name, T value) where T : struct
        {
            if (constantBuffers.TryGetValue(name, out ConstantBufferPayload? payload) &&
                payload is ConstantBufferPayload<T> cpayload)
            {
                cpayload.Set(value);
            }
            else
            {
                ConstantBufferPayload<T> payloadNew = new ConstantBufferPayload<T>();
                payloadNew.Set(value);
                constantBuffers[name] = payloadNew;
            }
        }

        public static Dictionary<int, string> EnumAdapters()
        {
            Dictionary<int, string> ret = new Dictionary<int, string>();

            using (SharpDX.DXGI.Factory f = new SharpDX.DXGI.Factory1())
            {
                int numAdapters = f.GetAdapterCount();

                for (int i = 0; i < numAdapters; i++)
                    ret[i] = f.GetAdapter(i).Description.Description;
            }

            return ret;
        }
    }
}
