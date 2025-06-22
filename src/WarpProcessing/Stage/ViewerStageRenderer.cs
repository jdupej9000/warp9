using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;
using Warp9.Viewer;

namespace Warp9.Stage
{
    public class ViewerStageRenderer
    {
        public ViewerStageRenderer(Project proj)
        {
            Project = proj;
        }

        protected RenderItemMesh meshRend = new RenderItemMesh();
        protected RenderItemGrid gridRend = new RenderItemGrid();
        protected ViewerStage stage = new ViewerStage();
       
        public Project Project { get; private init; }
        public RendererBase? Renderer { get; private set; }
        public ViewerStage Stage
        {
            get { return stage; }
            set { stage = value; UpdateFull(); }
        }

        public void AttachToRenderer(RendererBase rend)
        {
            if (rend != Renderer)
            {
                DetachRenderer();
                Renderer = rend;

                Renderer.AddRenderItem(meshRend);
                Renderer.AddRenderItem(gridRend);
            }
        }

        public void DetachRenderer()
        {
            if (Renderer is not null)
            {
                Renderer.ClearRenderItems();
                Renderer = null;
            }
        }

        private void UpdateFull()
        {
        }
    }
}
