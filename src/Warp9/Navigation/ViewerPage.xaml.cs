using System.CodeDom;
using System.IO;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Security.RightsManagement;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using Warp9.Controls;
using Warp9.Data;
using Warp9.IO;
using Warp9.JsonConverters;
using Warp9.ProjectExplorer;
using Warp9.Scene;
using Warp9.Viewer;

namespace Warp9.Navigation
{
    /// <summary>
    /// Interaction logic for ViewerPage.xaml
    /// </summary>
    public partial class ViewerPage : Page, IWarp9View
    {
        public ViewerPage(Window owner)
        {
            InitializeComponent();
            this.owner = owner;

            using FileStream fontStream = new FileStream(Path.Combine("Assets", "segoe-ui-minimal.fnt"), FileMode.Open, FileAccess.Read);
            font = FontDefinition.FromStream(fontStream, "Assets");
            hud = new RenderItemHud(font);

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
        WpfInteropRenderer? renderer = null;
        ViewerSceneRenderer? sceneRend = null;
        Warp9ViewModel? viewModel = null;
        IViewerContent? content = null;
        ICameraControl cameraControl;
        ViewProjConst vpc = new ViewProjConst();
        CameraLightConst clp = new CameraLightConst();
        TimeSpan lastRender = TimeSpan.Zero;
        Vector2 viewportSize = Vector2.One;
        FontDefinition font;
        RenderItemHud hud;
        
        bool mustMakeTarget = false;
        int viewDirty = 1;

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

        public void SetContent(params IViewerContent[] content)
        {
            cmbVis.Items.Clear();
            
            foreach (var item in content)
                cmbVis.Items.Add(item);

            if(cmbVis.Items.Count > 0)
                cmbVis.SelectedIndex = 0;

            Interlocked.Exchange(ref viewDirty, 1);
        }

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
            Themes.ThemesController.ThemeChanged += ThemesController_ThemeChanged;
        }

        public void DetachViewModel()
        {
            UnsetContent();
            viewModel = null;
            Themes.ThemesController.ThemeChanged -= ThemesController_ThemeChanged;
        }

        private void ThemesController_ThemeChanged(object? sender, EventArgs e)
        {
            if (renderer is not null)
                renderer.CanvasColor = Themes.ThemesController.GetColor("Brush.Background");
        }

        private void CameraControl_UpdateView(object? sender, CameraInfo e)
        {
            content?.ViewChanged(e);
            Interlocked.Exchange(ref viewDirty, 1);
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size size = WpfSizeToPixels(ImageGrid);
            InteropImage.SetPixelSize((int)size.Width, (int)size.Height);
            viewportSize = new Vector2((float)size.Width, (float)size.Height);

            content?.ViewportResized(new System.Drawing.Size((int)size.Width, (int)size.Height));
            cameraControl?.ResizeViewport(new Vector2((float)size.Width, (float)size.Height));
        }

        private void EnsureRenderer()
        {
            if (renderer is null)
            {
                if (!WpfInteropRenderer.TryCreate(0, out renderer) || renderer is null)
                    throw new InvalidOperationException();

                renderer.CanvasColor = Themes.ThemesController.GetColor("Brush.Background");
                //if (FindResource("ThemeColors.Window.Background") is Color clr)
                //    renderer.CanvasColor = System.Drawing.Color.FromArgb(clr.R, clr.G, clr.B);
                //else
                //    renderer.CanvasColor = System.Drawing.Color.Black;

                renderer.Fussy = false;
                renderer.Shaders.AddShaders(StockShaders.AllShaders);

                ModelConst mc = new ModelConst();
                mc.model = Matrix4x4.Identity;
                renderer.SetConstant(StockShaders.Name_ModelConst, mc);

                cameraControl.Get(out Matrix4x4 viewMat);
                Matrix4x4.Invert(viewMat, out Matrix4x4 viewMati);
                Vector3 camera = new Vector3(viewMati.M41, viewMati.M42, viewMati.M43);
               
                vpc.viewProj = Matrix4x4.Transpose(viewMat *
                    Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 3, 1, 0.01f, 100.0f));

                vpc.camera = new Vector4(camera, 1);
                renderer.SetConstant(StockShaders.Name_ViewProjConst, vpc);

                clp.cameraPos = camera;
                clp.lightPos = camera;
                renderer.SetConstant(StockShaders.Name_CameraLightConst, clp);

                PshConst pc = new PshConst();
                pc.color = new Vector4(0, 1, 0, 1);
                pc.ambStrength = 0.2f;
                pc.flags = 0;
                renderer.SetConstant(StockShaders.Name_PshConst, pc);

                InteropImage.WindowOwner = (new System.Windows.Interop.WindowInteropHelper(owner)).Handle;
                InteropImage.IsFrontBufferAvailableChanged += InteropImage_IsFrontBufferAvailableChanged;
                InteropImage.OnRender += OnRender;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureRenderer();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
            mustMakeTarget = true;
            InteropImage.RequestRender();
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

            renderer.Present();
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

        private void DisplayBlank()
        {
            UnsetContent();

            EnsureRenderer();
            if (renderer is null)
                throw new InvalidOperationException();

            renderer.ClearRenderItems();

            frmSidebar.Content = null;
            this.content = null;
        }

        public void DisplayContent(IViewerContent content)
        {
            UnsetContent();

            EnsureRenderer();
            if (renderer is null)
                throw new InvalidOperationException();

            renderer.ClearRenderItems();
            content.AttachRenderer(renderer);
            /*renderer.AddRenderItem(hud);

            hud.SetSubText(0, "HUD", 12, System.Drawing.Color.White,
                new System.Drawing.RectangleF(0, 0, 500, 100),
                false, Utils.TextRenderFlags.AlignLeft);*/

            Page? sidebar = content.GetSidebar();
            if (sidebar is not null)
            {
                frmSidebar.Content = sidebar;
            }

            this.content = content;
            this.content.ViewUpdated += Content_ViewUpdated;

            cameraControl.Scroll(0); // fire the ViewChanged event
        }

        private void UnsetContent()
        {
            if (content is not null)
            {
                content.ViewUpdated -= Content_ViewUpdated;
                content.DetachRenderer();
                content = null;
            }
        }

        private void Content_ViewUpdated(object? sender, EventArgs e)
        {
            Interlocked.Exchange(ref viewDirty, 1);
            ImageHost.InvalidateVisual();
        }

        private void cmbVis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbVis.SelectedItem is IViewerContent vc)
            {
                DisplayContent(vc);
            }
            else if (cmbVis.Items.Count > 0 && cmbVis.Items[0] is IViewerContent vc0)
            {
                //cmbVis.SelectedIndex = 0;
                DisplayContent(vc0);
            }
            else
            {
                DisplayBlank();
            }
        }

        private void Snapshot_Click(object sender, RoutedEventArgs e)
        {
            if (content is null)
                return;

            viewModel?.AddSnapshot(content.Scene, comment:content.DescribeScene());
        }

        private void SetView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag as string, out int viewIndex))
            {   
                switch (viewIndex)
                {
                    case 0:
                        cameraControl.Execute(CameraCommand.SetFront);
                        break;

                    case 1:
                        cameraControl.Execute(CameraCommand.SetBack);
                        break;

                    case 2:
                        cameraControl.Execute(CameraCommand.SetLeft);
                        break;

                    case 3:
                        cameraControl.Execute(CameraCommand.SetRight);
                        break;

                    case 4:
                        cameraControl.Execute(CameraCommand.SetTop);
                        break;

                    case 5:
                        cameraControl.Execute(CameraCommand.SetBottom);
                        break;

                }
            }
        }
    }
}
