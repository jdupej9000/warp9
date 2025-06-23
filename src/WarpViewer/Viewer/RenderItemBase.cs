using SharpDX.Direct3D11;
using System;

namespace Warp9.Viewer
{
    public enum RenderJobInvalidation
    {
        None = 0,
        DynamicData = 1,
        Full = 0x7fffffff
    };

    public abstract class RenderItemBase
    {
        public RenderItemBase()
        {
        }

        public uint Version { get; set; }
        public uint DynamicVersion { get; set; }

        public virtual void Commit(RenderJobInvalidation level = RenderJobInvalidation.Full)
        {
            if (level == RenderJobInvalidation.DynamicData)
            {
                DynamicVersion++;
            }
            else if (level == RenderJobInvalidation.Full)
            {
                Version++;
            }
        }

        public bool UpdateRenderJob(ref RenderJob? job, DeviceContext ctx, ShaderRegistry shaders, ConstantBufferManager constBuffers)
        {
            bool modified = false;

            if(job is null)
            {
                job = new RenderJob(shaders, constBuffers);
                modified = true;
            }

            modified |= UpdateJobInternal(job, ctx);

            if (modified)
                job.CommitVersion(RenderJobInvalidation.Full, this);

            return modified;
        }

        public void PartialUpdate(RenderJobInvalidation kind, RenderJob job, DeviceContext ctx)
        {
            if (kind == RenderJobInvalidation.Full)
                throw new InvalidOperationException();

            if (kind != RenderJobInvalidation.None)
            {
                PartialUpdateJobInternal(kind, job, ctx);
                job.CommitVersion(kind, this);
            }
        }

        public virtual void UpdateConstantBuffers(RenderJob job)
        {
        }



        protected abstract bool UpdateJobInternal(RenderJob job, DeviceContext ctx);
        protected virtual void PartialUpdateJobInternal(RenderJobInvalidation kind, RenderJob job, DeviceContext ctx)
        {
        }
    }
}
