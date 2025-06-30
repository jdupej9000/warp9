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

    protected RenderItemMesh meshRend = new RenderItemMesh(false);
    protected RenderItemGrid gridRend = new RenderItemGrid();
    protected ViewerScene scene = new ViewerScene();
    protected bool rendererChanged = false;

    public Project Project { get; private init; }
    public RendererBase? Renderer { get; private set; }
    public ViewerScene Scene
    {
        get { return scene; }
        set { scene = value; }
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
            rendererChanged = true;
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

    public override string ToString()
    {
        return string.Format("m0:({0}) g:({1})",
            meshRend.Version, gridRend.Version);
    }

    private void Renderer_Presenting(object? sender, PresentingInfo e)
    {
        if (Renderer is null)
            return;

        UpdateRenderItem(Scene.Mesh0, meshRend);
        UpdateRenderItem(Scene.Grid, gridRend);
        UpdateConstant(e);

        rendererChanged = false;
    }

    private void UpdateRenderItem(ISceneElement? elem, RenderItemBase ri)
    {
        if (elem is not null)
        {
            RenderItemDelta delta = ri.Version.Upgrade(elem.Version);

            if (rendererChanged)
                delta = RenderItemDelta.Full;

            elem.ConfigureRenderItem(delta, Project, ri);
        }
    }

    private void UpdateConstant(PresentingInfo pi)
    {
        if (Renderer is null)
            return;

        Matrix4x4.Invert(Scene.ViewMatrix, out Matrix4x4 viewInv);
        Vector3 camera = viewInv.Translation;
        float aspect = (float)pi.ViewportSize.Width / (float)pi.ViewportSize.Height;

        ModelConst mc = new ModelConst
        {
            model = Matrix4x4.Identity
        };
        Renderer.SetConstant(StockShaders.Name_ModelConst, mc);

        ViewProjConst vpc = new ViewProjConst
        {
            viewProj = Matrix4x4.Transpose(Scene.ViewMatrix *
               Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 3, aspect, 0.01f, 1000.0f)),

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
