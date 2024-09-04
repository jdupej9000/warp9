using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace Warp9.Viewer
{
    [Flags]
    public enum RasterizerMode
    {
        Solid = 0x0,
        Wireframe = 0x1,

        NoCull = 0x0,
        CullBack = 0x2,

        Invalid = 0x7fffffff
    }

    [Flags]
    public enum BlendMode
    {
        Default = 0x0,

        Invalid = 0x7fffffff
    }

    [Flags]
    public enum DepthMode
    {
        UseDepth = 0x0,
        NoDepth = 0x1,

        Invalid = 0x7fffffff
    }

    public class StateCache
    {
        public StateCache(Device dev)
        {
            rasterizerStateCache = new ObjectCache<RasterizerMode, RasterizerState>(
                (m) => CreateRasterizerState(dev, m));

            blendStateCache = new ObjectCache<BlendMode, BlendState>(
                (m) => CreateBlendState(dev, m));

            depthStateCache = new ObjectCache<DepthMode, DepthStencilState>(
                (m) => CreateDepthState(dev, m));
        }

        private ObjectCache<RasterizerMode, RasterizerState> rasterizerStateCache;
        private ObjectCache<BlendMode, BlendState> blendStateCache;
        private ObjectCache<DepthMode, DepthStencilState> depthStateCache;

        public ObjectCache<RasterizerMode, RasterizerState> RasterizerStateCache => rasterizerStateCache;
        public ObjectCache<BlendMode, BlendState> BlendStateCache => blendStateCache;
        public ObjectCache<DepthMode, DepthStencilState> DepthStateCache => depthStateCache;

        public void ResetLastState()
        {
            rasterizerStateCache.LastState = RasterizerMode.Invalid;
        }

        public static RasterizerState CreateRasterizerState(Device device, RasterizerMode mode)
        {
            RasterizerStateDescription desc = new RasterizerStateDescription();

            if (mode.HasFlag(RasterizerMode.Wireframe))
                desc.FillMode = FillMode.Wireframe;
            else
                desc.FillMode = FillMode.Solid;

            if (mode.HasFlag(RasterizerMode.CullBack))
                desc.CullMode = CullMode.Back;
            else
                desc.CullMode = CullMode.None;

            return new RasterizerState(device, desc);
        }

        public static BlendState CreateBlendState(Device device, BlendMode mode)
        {
            BlendStateDescription desc = new BlendStateDescription();

            return new BlendState(device, desc);
        }

        public static DepthStencilState CreateDepthState(Device device, DepthMode mode)
        {
            DepthStencilStateDescription desc = new DepthStencilStateDescription();

            if (mode.HasFlag(DepthMode.NoDepth))
                desc.IsDepthEnabled = false;
            else
                desc.IsDepthEnabled = true;

            return new DepthStencilState(device, desc);
        }
    }
}
