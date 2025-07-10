using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;
using Warp9.Scene;
using Warp9.Viewer;

namespace Warp9.Utils
{
    public class SnapshotRenderer
    {
        private SnapshotRenderer(Project proj, HeadlessRenderer rend, GalleryRenderSettings settings)
        {
            this.settings = settings;

            renderer = rend;
            renderer.CanvasColor = settings.BackgroundColor;
            renderer.Shaders.AddShader(StockShaders.VsDefault);
            renderer.Shaders.AddShader(StockShaders.VsDefaultInstanced);
            renderer.Shaders.AddShader(StockShaders.PsDefault);
            
            sceneRenderer = new ViewerSceneRenderer(proj);
            sceneRenderer.AttachToRenderer(renderer);
        }

        HeadlessRenderer renderer;
        ViewerSceneRenderer sceneRenderer;
        GalleryRenderSettings settings;

        private void Render(SnapshotInfo info)
        {
            SetSize(info);

            sceneRenderer.Scene = ApplyMods(info.Scene);

            renderer.Present();
            Bitmap bmp = renderer.ExtractColorAsBitmap();
            SaveBitmap(bmp, info);
        }

        private void SetSize(SnapshotInfo info)
        {
            RasterInfo ri;

            if (settings.ModResolution)
            {
                ri = new RasterInfo(settings.ModResolutionWidth, settings.ModResolutionHeight);
            }
            else if (settings.ModResolutionAspect)
            {
                throw new NotImplementedException();
            }
            else
            {
                ri = new RasterInfo(info.Scene.Viewport.Width, info.Scene.Viewport.Height);
            }

            renderer.RasterFormat = ri;
        }

        private ViewerScene ApplyMods(ViewerScene scene)
        {
            ViewerScene ret = scene.Duplicate();

            if (settings.ModDisableGrid && ret.Grid is not null)
                ret.Grid.Visible = false;

            return scene;
        }

        private void SaveBitmap(Bitmap bmp, SnapshotInfo info)
        {
            string fileName = info.Name + GetResultExtension();
            string path = Path.Combine(settings.Directory, fileName);

            bmp.Save(path);
        }

        private string GetResultExtension()
        {
            return settings.FormatIndex switch
            {
                0 => ".png",
                _ => ".png"
            };
        }

        public static void RenderSnaphots(Project proj, IReadOnlyList<SnapshotInfo> snapshots, GalleryRenderSettings settings)
        {
            if (!HeadlessRenderer.TryCreate(settings.AdapterIndex, out HeadlessRenderer? rend) || rend is null)
                throw new Exception("Could not initialize a headless renderer.");

            SnapshotRenderer sr = new SnapshotRenderer(proj, rend, settings);
            foreach (SnapshotInfo info in snapshots)
                sr.Render(info);

            rend.Dispose();
        }
    }
}
