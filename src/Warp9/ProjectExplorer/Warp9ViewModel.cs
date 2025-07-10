using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Windows.Forms;
using Warp9.Data;
using Warp9.Forms;
using Warp9.Jobs;
using Warp9.JsonConverters;
using Warp9.Model;
using Warp9.Processing;
using Warp9.Scene;
using Warp9.Utils;
using Warp9.Viewer;

namespace Warp9.ProjectExplorer
{
    public class Warp9ViewModel : INotifyPropertyChanged, IDisposable
    {
        public Warp9ViewModel(Project project, IWarp9Model model) 
        {
            Project = project;
            Model = model;

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
        public IWarp9Model Model { get; init; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Update()
        {
            foreach (ProjectItem pi in Items)
                pi.Update();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
        }

        public void Dispose()
        {
            renderer.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Save(bool forceNewPath = false)
        {
            string? currentArchivePath = Project.Archive?.FileName;
            string? destPath = null;
            if (!forceNewPath && currentArchivePath is not null)
            {
                destPath = currentArchivePath;
            }
            else
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Warp9 Project Files (*.w9)|*.w9";

                DialogResult res = dlg.ShowDialog();
                if (res == DialogResult.OK)
                    destPath = dlg.FileName;
            }

            if (destPath != null)
                Model.Save(destPath);
        }

        public void AddSnapshot(ViewerScene scene, string? name = null, string? filter = null, string? comment = null)
        {
            // TODO: maybe this should not be in view model

            // Create the snapshot in the project.
            SnapshotInfo si = Project.AddNewSnapshot();
            si.Scene = scene.Duplicate();
            si.Name = name ?? string.Format("img{0:D6}", si.Id);
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
            long thumbnailKey = Project.AddReferenceDirect(ProjectReferenceFormat.PngImage, thumbnail);
            si.ThumbnailKey = thumbnailKey;
        }

        public void ImportSpecTable()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Comma separated values (*.csv)|*.csv";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            CsvImporter importer = CsvImporter.Create(dlg.FileName);

            ImportCsvWindow importDlg = new ImportCsvWindow();
            importDlg.AttachImporter(importer);
            importDlg.ShowDialog();
            if (importDlg.DialogResult != true) return;

            SpecimenTableImportWindow assignDlg = new SpecimenTableImportWindow();
            assignDlg.AttachImporter(importer);
            assignDlg.ShowDialog();
            if (assignDlg.DialogResult != true) return;

            SpecimenTable specTab = SpecimenTableGenerator.FromImporter(importer, assignDlg.ImportOperations, Project);
            ProjectEntry entry = Project.AddNewEntry(ProjectEntryKind.Specimens);
            entry.Name = "Imported";
            entry.Payload.Table = specTab;

            Update();
        }


        public void RenderSnapshots(IReadOnlyList<SnapshotInfo> snapshots)
        {
            GalleryRenderSettings settings = new GalleryRenderSettings();
            settings.ModViewList.AddRange(snapshots.Select((s) => s.Name));

            RenderSettingsWindow dlg = new RenderSettingsWindow();
            dlg.AttachSettings(settings);
            if (dlg.ShowDialog().GetValueOrDefault() != true)
                return;

            Model.StartJob(RenderGalleryJob.Create(snapshots, settings), "Render gallery");
        }

        public void ComputeDca()
        {
            DcaConfiguration config = new DcaConfiguration();

            DcaConfigWindow cfgWnd = new DcaConfigWindow();
            cfgWnd.Attach(Project, config);
            cfgWnd.ShowDialog();

            if (cfgWnd.DialogResult is null || cfgWnd.DialogResult == false)
                return;

            Model.StartJob(DcaJob.Create(config, Project), "DCA");
        }

        public void ComputeDcaPca()
        {
            PcaConfiguration config = new PcaConfiguration();

            PcaConfigWindow cfgWnd = new PcaConfigWindow();
            cfgWnd.Attach(Project, config);
            cfgWnd.ShowDialog();

            if (cfgWnd.DialogResult is null || cfgWnd.DialogResult == false)
                return;

            Model.StartJob(PcaJob.CreateDcaPca(config, Project), "High dimensional PCA");
        }
    }
}
