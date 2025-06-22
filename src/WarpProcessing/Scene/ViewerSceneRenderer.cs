using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        UpdateConstant();
    }

    private void UpdateConstant()
    {
        if (Renderer is null)
            return;

        ModelConst mc = new ModelConst();
        mc.model = Matrix4x4.Identity;
        Renderer.SetConstant(StockShaders.Name_ModelConst, mc);

        Vector3 camera = new Vector3(1.0f, 2.0f, 3.0f);
        Vector3 at = new Vector3(0, 0, 0);
        Vector3 up = new Vector3(0, 1, 0);
        ViewProjConst vpc = new ViewProjConst();
        vpc.viewProj = Matrix4x4.Transpose(Scene.ViewMatrix *
           Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 3, 1, 0.01f, 100.0f));

        vpc.camera = new Vector4(camera, 1);
        Renderer.SetConstant(StockShaders.Name_ViewProjConst, vpc);

        CameraLightConst clp = new CameraLightConst();
        clp.cameraPos = camera;
        clp.lightPos = camera;
        Renderer.SetConstant(StockShaders.Name_CameraLightConst, clp);

        PshConst pc = new PshConst();
        pc.color = new Vector4(0, 1, 0, 1);
        pc.ambStrength = 0.2f;
        pc.flags = 0;
        Renderer.SetConstant(StockShaders.Name_PshConst, pc);

    }
}
