using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Viewer
{
    public class RenderItemMesh : RenderItemBase
    {
        public RenderItemMesh()
        {
        }

        Mesh? mesh;
        Lut? lut;

        public Mesh? Mesh
        {
            get { return mesh; }
            set { mesh = value; Commit(); }
        }

        public Lut? Lut
        {
            get { return lut; }
            set { lut = value; Commit(); }
        }

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            return true;
        }

        public override void UpdateConstantBuffers(RenderJob job)
        {
            base.UpdateConstantBuffers(job);
        }
    }
}
