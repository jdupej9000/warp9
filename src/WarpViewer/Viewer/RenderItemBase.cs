using SharpDX.Direct3D11;

namespace Warp9.Viewer
{
    public abstract class RenderItemBase
    {
        public RenderItemBase()
        {
        }

        public uint Version { get; private set; }

        public virtual void Commit()
        {
            Version++;
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
                job.CommitVersion(Version);

            return modified;
        }

        public virtual void UpdateConstantBuffers(RenderJob job)
        {
        }

        protected abstract bool UpdateJobInternal(RenderJob job, DeviceContext ctx);
    }
}
