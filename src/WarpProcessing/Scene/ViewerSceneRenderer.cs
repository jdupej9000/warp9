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
            Renderer.Presenting += Renderer_Presenting;

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
            Renderer.Presenting -= Renderer_Presenting;
            Renderer = null;
        }
    }

    private void Renderer_Presenting(object? sender, EventArgs e)
    {
        UpdateConstant();
        UpdateDynamic();
    }

    private void UpdateFull()
    {
        Scene.Mesh0?.ConfigureRenderItem(Project, meshRend);
        Scene.Grid?.ConfigureRenderItem(Project, gridRend);
    }

    private void UpdateDynamic()
    {
        Scene.Mesh0?.UpdateDynamicBuffers(Project, meshRend);
        Scene.Grid?.UpdateDynamicBuffers(Project, gridRend);
    }

    private void UpdateConstant()
    {
        if (Renderer is null)
            return;

        Matrix4x4.Invert(Scene.ViewMatrix, out Matrix4x4 viewInv);
        Vector3 camera = viewInv.Translation;

        ModelConst mc = new ModelConst
        {
            model = Matrix4x4.Identity
        };
        Renderer.SetConstant(StockShaders.Name_ModelConst, mc);

        ViewProjConst vpc = new ViewProjConst
        {
            viewProj = Matrix4x4.Transpose(Scene.ViewMatrix *
               Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 3, 1, 0.01f, 100.0f)),

            camera = new Vector4(camera, 1)
        };
        Renderer.SetConstant(StockShaders.Name_ViewProjConst, vpc);

        CameraLightConst clp = new CameraLightConst
        {
            cameraPos = camera,
            lightPos = camera
        };
        Renderer.SetConstant(StockShaders.Name_CameraLightConst, clp);

        PshConst pc = new PshConst
        {
            color = new Vector4(0, 1, 0, 1),
            ambStrength = 0.2f,
            flags = 0
        };
        Renderer.SetConstant(StockShaders.Name_PshConst, pc);
    }
}
