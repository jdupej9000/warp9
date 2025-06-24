using SharpDX.Direct3D11;
using System;

namespace Warp9.Viewer
{
    public abstract class RenderItemBase
    {
        public RenderItemBase()
        {
        }

        public RenderItemVersion Version { get; } = new RenderItemVersion();
        public bool AutoCommit { get; set; } = true;

        public RenderItemDelta UpdateRenderJob(ref RenderJob? job, DeviceContext ctx, ShaderRegistry shaders, ConstantBufferManager constBuffers)
        {
            if(job is null)
                job = new RenderJob(shaders, constBuffers);

            RenderItemDelta ret = job.Version.Upgrade(Version);

            if (ret == RenderItemDelta.Full)
                UpdateJobInternal(job, ctx);
            else if (ret != RenderItemDelta.None)
                PartialUpdateJobInternal(ret, job, ctx);
            
            return ret;
        }

        public virtual void UpdateConstantBuffers(RenderJob job)
        {
        }

        protected abstract bool UpdateJobInternal(RenderJob job, DeviceContext ctx);
        protected virtual void PartialUpdateJobInternal(RenderItemDelta kind, RenderJob job, DeviceContext ctx)
        {
        }

        protected void Commit(RenderItemDelta delta = RenderItemDelta.Full)
        {
            if(AutoCommit)
                Version.Commit(delta);
        }
    }
}
