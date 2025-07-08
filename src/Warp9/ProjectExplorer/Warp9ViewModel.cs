using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Warp9.Data;
using Warp9.JsonConverters;
using Warp9.Model;
using Warp9.Scene;
using Warp9.Viewer;

namespace Warp9.ProjectExplorer
{
    public class Warp9ViewModel : INotifyPropertyChanged
    {
        public Warp9ViewModel(Project project) 
        {
            Project = project;

            if (!HeadlessRenderer.TryCreate(0, out HeadlessRenderer? rend))
                throw new Exception("Could not initialize a headless renderer.");

            renderer = rend;
            renderer.CanvasColor = Color.Transparent;
            renderer.Shaders.AddShader(StockShaders.VsDefault);
            renderer.Shaders.AddShader(StockShaders.VsDefaultInstanced);
            renderer.Shaders.AddShader(StockShaders.PsDefault);
            renderer.RasterFormat = new RasterInfo(256, 256);
            sceneRenderer = new ViewerSceneRenderer(project);
            sceneRenderer.AttachToRenderer(renderer);

            Items.Add(new GeneralProjectItem(this));
            Items.Add(new DatasetsProjectItem(this));
            Items.Add(new ResultsProjectItem(this));
            Items.Add(new GalleryProjectItem(this));
        }

        HeadlessRenderer renderer;
        ViewerSceneRenderer sceneRenderer;

        public ObservableCollection<ProjectItem> Items { get; init; } = new ObservableCollection<ProjectItem>();
        public Project Project { get; init; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Update()
        {
            foreach (ProjectItem pi in Items)
                pi.Update();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
        }

        public void AddSnapshot(ViewerScene scene, string? name = null, string? filter = null, string? comment = null)
        {
            // TODO: maybe this should not be in view model
            // TODO: clone the scene

            // Create the snapshot in the project.
            SnapshotInfo si = Project.AddNewSnapshot();
            si.Scene = scene;
            si.Name = name ?? DateTime.Now.ToString();
            si.Filter = filter;
            si.Comment = comment;

            // All data that exists only in arrays and buffers must be converted into serializable
            // objects and added into the project as references.
            foreach (ISceneElement sceneElem in scene.EnumSceneElements())
                sceneElem.PersistData(Project);

            // Render the thumbnail and add it as a reference into the project.
            sceneRenderer.Scene = scene;
            renderer.Present();
            Bitmap thumbnail = renderer.ExtractColorAsBitmap();
            long thumbnailKey = Project.AddReferenceDirect<Bitmap>(ProjectReferenceFormat.PngImage, thumbnail);
            si.ThumbnailKey = thumbnailKey;
        }
    }
}
