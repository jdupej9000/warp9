using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Warp9.Viewer
{
    public class RendererBase
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

        public ShaderRegistry Shaders => shaders;
        public string DeviceName => deviceDesc.Description;
        public Color CanvasColor { get; set; }

        public event EventHandler? Presenting;

        public void AddRenderItem(RenderItemBase renderItem)
        {
            lock (renderItems)
            {
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

        protected void Render()
        {
            if (device is null || ctx is null || stateCache is null)
                throw new InvalidOperationException();

            Presenting?.Invoke(this, EventArgs.Empty);

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

                // Restore constant buffer baselines for all render items.
                foreach (var kvp in constantBuffers)
                    constantBufferManager.Set(ctx, kvp.Key, kvp.Value);

                // force setting the rasterizer state at least once
                stateCache.ResetLastState();

                foreach (var kvp in renderItems)
                {
                    if (kvp.Value is not null)
                    {
                        // Update individual or per-drawcall vertex buffers.
                        kvp.Key.UpdateConstantBuffers(kvp.Value);
                        RenderJobExecuteStatus renderStatus = kvp.Value.Render(ctx, stateCache);

                        if (renderStatus != RenderJobExecuteStatus.Ok)
                        {

#if DEBUG
    if(renderStatus != RenderJobExecuteStatus.Ok)
        throw new InvalidOperationException($"Render job of {kvp.Key.ToString()} failed with {renderStatus}.");
#else
                            Console.Error.WriteLine($"Render job of {kvp.Key.ToString()} failed with {renderStatus}.");
#endif
                        }
                    }
                }
            }
        }

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
