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
            renderer.RasterFormat = new RasterInfo(128, 128);
            sceneRenderer = new ViewerSceneRenderer(project);
            sceneRenderer.AttachToRenderer(renderer);

            Items.Add(new GeneralProjectItem(this));
            Items.Add(new DatasetsProjectItem(this));
            Items.Add(new ResultsProjectItem(this));
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

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Items"));
        }

        public void AddSnapshot(ViewerScene scene)
        {
            JsonSerializerOptions opts = new JsonSerializerOptions()
            {
                AllowTrailingCommas = false,
                //WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            opts.Converters.Add(new SpecimenTableJsonConverter());
            opts.Converters.Add(new ReferencedDataJsonConverter<Mesh>());
            opts.Converters.Add(new ReferencedDataJsonConverter<float[]>());
            opts.Converters.Add(new ReferencedDataJsonConverter<Vector3[]>());
            opts.Converters.Add(new ReferencedDataJsonConverter<PointCloud>());
            opts.Converters.Add(new ReferencedDataJsonConverter<Data.Matrix>());
            opts.Converters.Add(new ReferencedDataJsonConverter<System.Drawing.Bitmap>());
            opts.Converters.Add(new LutSpecJsonConverter());
            opts.Converters.Add(new ColorJsonConverter());
            opts.Converters.Add(new Matrix4x4JsonConverter());
            opts.Converters.Add(new SizeJsonConverter());

            using FileStream fs = new FileStream("viewer-result.json", FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, scene, opts);

            sceneRenderer.Scene = scene;
            renderer.Present();
            using (Bitmap bmp = renderer.ExtractColorAsBitmap())
                bmp.Save("viewer-result.png");
        }
    }
}
