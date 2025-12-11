using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Forms;
using Warp9.Model;
using Warp9.ProjectExplorer;
using Warp9.Viewer;

namespace Warp9.Navigation
{
    /// <summary>
    /// Interaction logic for SpecimenEditorPage.xaml
    /// </summary>
    public partial class SpecimenEditorPage : Page, IWarp9View
    {
        public SpecimenEditorPage(Window owner)
        {
            InitializeComponent();
            this.owner = owner;

            using FileStream fontStream = new FileStream(
                System.IO.Path.Combine("Assets", "segoe-ui-minimal.fnt"), 
                FileMode.Open, FileAccess.Read);

            font = FontDefinition.FromStream(fontStream, "Assets");
            itemHud = new RenderItemHud(font);

            ICameraControl ctl = Options.Instance.CameraControlIndex switch
            {
                0 => new EulerCameraControl(),
                1 => new ArcBallCameraControl(),
                2 => new PlaneCameraControl(),
                _ => new EulerCameraControl()
            };

            SetCameraControl(ctl);
        }

        Window owner;
        Warp9ViewModel? viewModel;
        WpfInteropRenderer? renderer = null;
        RenderItemGrid? itemGrid = null;
        RenderItemMesh? itemMesh = null;
        RenderItemInstancedMesh? itemLms = null;
        RenderItemHud itemHud;
        FontDefinition font;
        ICameraControl cameraControl;
        ViewProjConst vpc = new ViewProjConst();
        CameraLightConst clp = new CameraLightConst();
        TimeSpan lastRender = TimeSpan.Zero;
        Vector2 viewportSize = Vector2.One;
        Vector3 sceneCenter = Vector3.Zero;
        DateTime lastHudUpdate = DateTime.MinValue;
        long lastFrameCount = 0;
        long entryIndex = -1;

        bool mustMakeTarget = false;
        int viewDirty = 1;

        readonly static System.Drawing.Color hudColor = System.Drawing.Color.SlateGray;
        readonly static float hudSize = 14.0f;

        SpecimenTable Table
        {
            get
            {
                if (entryIndex < 0 ||
                    viewModel is null ||
                    !viewModel.Project.Entries.TryGetValue(entryIndex, out ProjectEntry? entry))
                    throw new InvalidOperationException();

                return entry.Payload.Table ?? throw new InvalidOperationException();
            }
        }

        Project Project => viewModel?.Project ?? throw new InvalidOperationException();

        public void SetCameraControl(ICameraControl cctl)
        {
            if (cameraControl is not null)
            {
                cameraControl.UpdateView -= CameraControl_UpdateView;
                cameraControl.Get(out Matrix4x4 mat);
                cctl.Set(mat);
            }

            cameraControl = cctl;
            cameraControl.ResizeViewport(new Vector2((float)Width, (float)Height));
            cameraControl.UpdateView += CameraControl_UpdateView;
        }

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
            Themes.ThemesController.ThemeChanged += ThemesController_ThemeChanged;
        }

        public void DetachViewModel()
        { 
            viewModel = null;
            Themes.ThemesController.ThemeChanged -= ThemesController_ThemeChanged;
        }

        private void ThemesController_ThemeChanged(object? sender, EventArgs e)
        {
            if(renderer is not null)
                renderer.CanvasColor = Themes.ThemesController.GetColor("Brush.Background");
        }

        public void ShowEntry(long idx)
        {
            dataMain.Columns.Clear();

            entryIndex = idx;
            SpecimenTable table = Table;

            PopulateColumnsTable();
            PopulateSpecimenTable();
        }

        private void PopulateColumnsTable()
        {
            lvCols.ItemsSource = Table.Columns;
        }

        private void PopulateSpecimenTable()
        {
            dataMain.ItemsSource = Table;

            DataGridTextColumn colId = new DataGridTextColumn
            {
                Header = new SpecimenTableColumnInfo("ID", ""),
                Binding = new Binding("[!index]"),
                CanUserReorder = false,
                IsReadOnly = true
            };
            dataMain.Columns.Add(colId);

            foreach (var kvp in Table.Columns)
            {
                switch (kvp.Value.ColumnType)
                {
                    case SpecimenTableColumnType.Integer:
                    case SpecimenTableColumnType.Real:
                    case SpecimenTableColumnType.String:
                        {
                            DataGridTextColumn col = new DataGridTextColumn
                            {
                                Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("[" + kvp.Key + "]")
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Factor:
                        {
                            DataGridComboBoxColumn col = new DataGridComboBoxColumn
                            {
                                Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                SelectedItemBinding = new Binding("[" + kvp.Key + "]"),
                                ItemsSource = kvp.Value.Names
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Boolean:
                        {
                            DataGridCheckBoxColumn col = new DataGridCheckBoxColumn
                            {
                                Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("[" + kvp.Key + "]")
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;
                    case SpecimenTableColumnType.Image:
                    case SpecimenTableColumnType.Mesh:
                    case SpecimenTableColumnType.PointCloud:
                    case SpecimenTableColumnType.Matrix:
                        {
                            DataGridTextColumn col = new DataGridTextColumn
                            {
                                Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("[" + kvp.Key + "]"),
                                IsReadOnly = true
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        private void btnSpecAdd_Click(object sender, RoutedEventArgs e)
        {
            SpecimenTable table = Table;
            table.Add(new SpecimenTableRow(table, table.Columns.Count));
        }

        private void btnSpecDelete_Click(object sender, RoutedEventArgs e)
        {
            int selected = dataMain.SelectedIndex;
            if (selected != -1)
                Table.RemoveAt(selected);
        }

        private void btnSpecExport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnColAdd_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnColDelete_Click(object sender, RoutedEventArgs e)
        {
        }

        private void CameraControl_UpdateView(object? sender, CameraInfo e)
        {
            //content?.ViewChanged(e);
            Interlocked.Exchange(ref viewDirty, 1);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureRenderer();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
            mustMakeTarget = true;
            InteropImage.RequestRender();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size size = WpfSizeToPixels(ImageGrid);
            InteropImage.SetPixelSize((int)size.Width, (int)size.Height);
            viewportSize = new Vector2((float)size.Width, (float)size.Height);

            cameraControl?.ResizeViewport(new Vector2((float)size.Width, (float)size.Height));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }

        void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;

            // It's possible for Rendering to call back twice in the same frame 
            // so only render when we haven't already rendered in this frame.
            // Also, limit to 60 fps
            if (Interlocked.Exchange(ref viewDirty, 0) != 0 ||
                (args.RenderingTime - lastRender).TotalSeconds >= 1.0)
            {
                InteropImage.RequestRender();
                lastRender = args.RenderingTime;
            }
        }

        void OnRender(IntPtr resourcePtr, bool isNewSurface)
        {
            if (renderer is null)
                throw new InvalidOperationException();

            if (isNewSurface || mustMakeTarget)
            {
                // a new surface has been created (e.g. after a resize)
                renderer.EnsureSharedBackBuffer(resourcePtr, WpfSizeToPixels(ImageGrid));
                mustMakeTarget = false;
            }

            cameraControl.Get(out Matrix4x4 viewMat);
            UpdateConstant(viewMat, viewportSize);
            renderer.Present();

            DateTime now = DateTime.Now;
            if ((now - lastHudUpdate).TotalSeconds > 1)
            {
                long curFrameCount = renderer.FrameIdx;
                double fps = (curFrameCount - lastFrameCount) / (now - lastHudUpdate).TotalSeconds;

                itemHud.SetSubText(0, string.Format("{0}\n{1:F1}fps ({2:F1}ms frame)",
                        renderer.DeviceName,
                        fps,
                        renderer.LastFrameTime.TotalMilliseconds),
                    hudSize, hudColor,
                    new System.Drawing.RectangleF(0, 0, 500, 100),
                    false, Utils.TextRenderFlags.AlignLeft);

                lastHudUpdate = now;
                lastFrameCount = curFrameCount;
            }
        }

        private void EnsureRenderer()
        {
            if (renderer is null)
            {
                if (!WpfInteropRenderer.TryCreate(0, out renderer) || renderer is null)
                    throw new InvalidOperationException();

                renderer.CanvasColor = Themes.ThemesController.GetColor("Brush.Background");             

                renderer.Fussy = false;
                renderer.Shaders.AddShaders(StockShaders.AllShaders);

                cameraControl.Get(out Matrix4x4 viewMat);
                UpdateConstant(viewMat, viewportSize);

                itemGrid = new RenderItemGrid();
                renderer.AddRenderItem(itemGrid);

                itemLms = new RenderItemInstancedMesh();
                itemLms.Mesh = MeshUtils.MakeCubeIndexed(1);
                renderer.AddRenderItem(itemLms);

                itemMesh = new RenderItemMesh();
                itemMesh.FillColor = System.Drawing.Color.White;
                itemMesh.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.DiffuseLighting | MeshRenderStyle.EstimateNormals;
                itemMesh.RenderFace = true;
                itemMesh.RenderDepth = true;
                itemMesh.RenderBlend = BlendMode.NoBlend;
                itemMesh.RenderCull = false;
                renderer.AddRenderItem(itemMesh);

                itemHud.SetSubText(0, "HUD", 12, hudColor, new System.Drawing.RectangleF(0, 0, 100, 100));
                itemHud.Order = 1000;
                renderer.AddRenderItem(itemHud);

                InteropImage.WindowOwner = (new System.Windows.Interop.WindowInteropHelper(owner)).Handle;
                InteropImage.IsFrontBufferAvailableChanged += InteropImage_IsFrontBufferAvailableChanged;
                InteropImage.OnRender += OnRender;
            }
        }

        private void UpdateConstant(Matrix4x4 view, Vector2 viewport)
        {
            if (renderer is null)
                return;

            Matrix4x4.Invert(view, out Matrix4x4 viewInv);
            Vector3 camera = viewInv.Translation;
            float aspect = viewport.X / viewport.Y;

            ModelConst mc = new ModelConst
            {
                model = Matrix4x4.Identity
            };
            renderer.SetConstant(StockShaders.Name_ModelConst, mc);

            ViewProjConst vpc = new ViewProjConst
            {
                viewProj = Matrix4x4.Transpose(view *
                   Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 3, aspect, 0.01f, 1000.0f)),

                camera = new Vector4(camera, 1)
            };
            renderer.SetConstant(StockShaders.Name_ViewProjConst, vpc);

            CameraLightConst clp = new CameraLightConst
            {
                cameraPos = camera,
                lightPos = camera
            };
            renderer.SetConstant(StockShaders.Name_CameraLightConst, clp);

            PshConst pc = new PshConst
            {
                color = new Vector4(0, 1, 0, 1),
                ambStrength = 0.2f,
                flags = 0
            };
            renderer.SetConstant(StockShaders.Name_PshConst, pc);
        }


        private void InteropImage_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
            {
                // force recreation of lost frontbuffer
                Size size = WpfSizeToPixels(ImageGrid);
                InteropImage.SetPixelSize((int)size.Width + 1, (int)size.Height + 1);
                InteropImage.SetPixelSize((int)size.Width, (int)size.Height);
                InteropImage.RequestRender();
            }

        }

        private static Size WpfSizeToPixels(FrameworkElement element)
        {
            var source = PresentationSource.FromVisual(element);
            System.Windows.Media.Matrix transformToDevice = source.CompositionTarget.TransformToDevice;

            return (Size)transformToDevice.Transform(new System.Windows.Vector(element.ActualWidth, element.ActualHeight));
        }


        private void InteropImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(this);
            if (e.ChangedButton != MouseButton.Right) return;

            bool shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            cameraControl?.Grab(new Vector2((float)pos.X, (float)pos.Y), shiftPressed);
        }

        private void InteropImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(this);
            if (e.ChangedButton != MouseButton.Right) return;

            cameraControl?.Release(new Vector2((float)pos.X, (float)pos.Y));
        }

        private void InteropImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(this);
            if (!e.RightButton.HasFlag(MouseButtonState.Pressed)) return;

            cameraControl?.Move(new Vector2((float)pos.X, (float)pos.Y));
        }

        private void InteropImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cameraControl?.Scroll(e.Delta);
        }

        private void lvCols_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(lvCols.SelectedItem is KeyValuePair<string, SpecimenTableColumn> kvp))
                return;

            ColumnEditWindow dlg = new ColumnEditWindow();
            dlg.ColumnName = kvp.Key;
            dlg.ColumnType = kvp.Value.ColumnType;
            dlg.ColumnLevels = kvp.Value.Names ?? Array.Empty<string>();

            dlg.Show();

            if (dlg.DialogResult ?? false != true)
                return;

            
        }

        private void dataMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 ||
                e.AddedItems[0] is not SpecimenTableRow row)
                return;

            Aabb? box = null;

            // TODO: cache the search and base it on which column is visible
            string? meshColumn = Table.Columns.FirstOrDefault((x) => x.Value.ColumnType == SpecimenTableColumnType.Mesh).Key;
            if (itemMesh is not null &&
                meshColumn is not null &&
                row[meshColumn] is long meshLnk &&
                Project.TryGetReference(meshLnk, out Mesh? mesh) &&
                mesh is not null)
            {
                box = MeshUtils.FindBoundingBox(mesh, MeshSegmentSemantic.Position);
                if (box is null)
                    throw new InvalidOperationException();

                itemMesh.ModelMatrix = Matrix4x4.CreateTranslation(-box.Value.Center);
                itemMesh.Mesh = mesh;
            }

            string? lmsColumn = Table.Columns.FirstOrDefault((x) => x.Value.ColumnType == SpecimenTableColumnType.PointCloud).Key;
            if (itemLms is not null &&
                lmsColumn is not null &&
                row[lmsColumn] is long lmsLnk &&
                Project.TryGetReference(lmsLnk, out PointCloud? lms) &&
                lms is not null)
            {
                if (box is null)
                    box = MeshUtils.FindBoundingBox(lms, MeshSegmentSemantic.Position);

                if (box is null)
                    throw new InvalidOperationException();

                itemLms.BaseModelMatrix = Matrix4x4.CreateTranslation(-box.Value.Center);
                itemLms.Instances = lms;
            }

            InteropImage.RequestRender();
        }
    }
}
