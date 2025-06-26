using SharpDX.Direct3D11;
using System;

namespace Warp9.Viewer
{
    public abstract class RenderItemBase
    {
        public RenderItemBase()
        {
        }

        public RenderItemVersion Version { get; } = new RenderItemVersion(1);
        public bool AutoCommit { get; set; } = true;

        public RenderItemDelta UpdateRenderJob(ref RenderJob? job, DeviceContext ctx, ShaderRegistry shaders, ConstantBufferManager constBuffers)
        {
            bool jobCreated = job is null;

            if(job is null)
                job = new RenderJob(shaders, constBuffers);

            RenderItemDelta ret = job.Version.Upgrade(Version);

            if (ret == RenderItemDelta.Full || jobCreated)
                UpdateJobInternal(job, ctx);
            
            if (ret != RenderItemDelta.None)
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
