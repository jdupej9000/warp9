using SharpDX.Direct3D11;
using System;

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

    [Flags]
    public enum SamplerMode
    {
        Nearest = 0x0,
        Linear = 0x1,
        Anisotropic = 0x2,

        Clamp = 0x0,

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

            samplerStateCache = new ObjectCache<SamplerMode, SamplerState>(
                (m) => CreateSamplerState(dev, m));
        }

        private ObjectCache<RasterizerMode, RasterizerState> rasterizerStateCache;
        private ObjectCache<BlendMode, BlendState> blendStateCache;
        private ObjectCache<DepthMode, DepthStencilState> depthStateCache;
        private ObjectCache<SamplerMode, SamplerState> samplerStateCache;

        public ObjectCache<RasterizerMode, RasterizerState> RasterizerStateCache => rasterizerStateCache;
        public ObjectCache<BlendMode, BlendState> BlendStateCache => blendStateCache;
        public ObjectCache<DepthMode, DepthStencilState> DepthStateCache => depthStateCache;
        public ObjectCache<SamplerMode, SamplerState> SamplerStateCache => samplerStateCache;

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

        public static SamplerState CreateSamplerState(Device device, SamplerMode mode)
        {
            SamplerStateDescription desc = new SamplerStateDescription();

            if (mode.HasFlag(SamplerMode.Anisotropic))
            {
                desc.Filter = Filter.Anisotropic;
                desc.MaximumAnisotropy = 16;
            }
            if (mode.HasFlag(SamplerMode.Linear))
                desc.Filter = Filter.MinMagMipLinear;
            else
                desc.Filter = Filter.MinMagMipPoint;

            desc.AddressV = TextureAddressMode.Clamp;
            desc.AddressU = TextureAddressMode.Clamp;
            desc.AddressW = TextureAddressMode.Clamp;
            
            return new SamplerState(device, desc);
        }
    }
}
