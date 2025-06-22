using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;
using Warp9.Viewer;

namespace Warp9.Scene;

public class ViewerSceneRenderer
{
    public ViewerSceneRenderer(Project proj)
    {
        Project = proj;
    }

    protected RenderItemMesh meshRend = new RenderItemMesh();
    protected RenderItemGrid gridRend = new RenderItemGrid();
    protected ViewerScene scene = new ViewerScene();
   
    public Project Project { get; private init; }
    public RendererBase? Renderer { get; private set; }
    public ViewerScene Scene
    {
        get { return scene; }
        set { scene = value; UpdateFull(); }
    }

    public void AttachToRenderer(RendererBase rend)
    {
        if (rend != Renderer)
        {
            DetachRenderer();
            Renderer = rend;

            Renderer.AddRenderItem(meshRend);
            Renderer.AddRenderItem(gridRend);

            UpdateFull();
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
        Scene.Mesh0?.ConfigureRenderItem(Project, meshRend);
        Scene.Grid?.ConfigureRenderItem(Project, gridRend);
    }
}
