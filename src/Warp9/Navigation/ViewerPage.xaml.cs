﻿using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Warp9.Controls;
using Warp9.Data;
using Warp9.IO;
using Warp9.ProjectExplorer;
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
        }

        Window owner;
        WpfInteropRenderer? renderer = null;
        RenderItemMesh? meshRend;
        TimeSpan lastRender = TimeSpan.Zero;
        bool mustMakeTarget = false;
        private Random rnd = new Random();

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size size = WpfSizeToPixels(ImageGrid);
            InteropImage.SetPixelSize((int)size.Width, (int)size.Height);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (renderer is null)
            {
                if (!WpfInteropRenderer.TryCreate(0, out renderer) || renderer is null)
                    throw new InvalidOperationException();

                if (owner.Background is SolidColorBrush bkb)
                {
                    renderer.CanvasColor = System.Drawing.Color.FromArgb(bkb.Color.R, bkb.Color.G, bkb.Color.B); 
                }
                else
                {
                    renderer.CanvasColor = System.Drawing.Color.Black;
                }

                renderer.Fussy = false;
                renderer.Shaders.AddShader(StockShaders.VsDefault);
                renderer.Shaders.AddShader(StockShaders.VsDefaultInstanced);
                renderer.Shaders.AddShader(StockShaders.PsDefault);

                ModelConst mc = new ModelConst();
                mc.model = Matrix4x4.Identity;
                renderer.SetConstant(StockShaders.Name_ModelConst, mc);

                Vector3 camera = new Vector3(1.0f, 2.0f, 3.0f);
                Vector3 at = new Vector3(0, 0, 0);
                Vector3 up = new Vector3(0, 1, 0);
                ViewProjConst vpc = new ViewProjConst();
                vpc.viewProj = Matrix4x4.Transpose(Matrix4x4.CreateLookAtLeftHanded(camera, at, up) *
                    Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 3, 1, 0.01f, 100.0f));

                vpc.camera = new Vector4(camera, 1);
                renderer.SetConstant(StockShaders.Name_ViewProjConst, vpc);

                CameraLightConst clp = new CameraLightConst();
                clp.cameraPos = camera;
                clp.lightPos = camera;
                renderer.SetConstant(StockShaders.Name_CameraLightConst, clp);

                PshConst pc = new PshConst();
                pc.color = new Vector4(0, 1, 0, 1);
                pc.ambStrength = 0.2f;
                pc.flags = 0;
                renderer.SetConstant(StockShaders.Name_PshConst, pc);

                meshRend = new RenderItemMesh();
                meshRend.Mesh = LoadObjAsset("suzanne.obj", ObjImportMode.AllUnshared);
                meshRend.Style = MeshRenderStyle.ColorFlat | MeshRenderStyle.PhongBlinn;
                meshRend.ModelMatrix = Matrix4x4.CreateScale(1.5f);
                meshRend.Color = System.Drawing.Color.Gray;
                renderer.AddRenderItem(meshRend);

                InteropImage.WindowOwner = (new System.Windows.Interop.WindowInteropHelper(owner)).Handle;
                InteropImage.IsFrontBufferAvailableChanged += InteropImage_IsFrontBufferAvailableChanged;
                InteropImage.OnRender += OnRender;
            }

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
            if ((args.RenderingTime - lastRender).TotalSeconds >= 1.0)
            {
                if (meshRend is not null)
                    meshRend.Color = System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));

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

        public static readonly string AssetsPath = @"../../test/data/";

        public static Stream OpenAsset(string name)
        {
            string path = System.IO.Path.Combine(AssetsPath, name);

            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }
        public static Mesh LoadObjAsset(string name, ObjImportMode mode)
        {
            using Stream s = OpenAsset(name);
            if (!ObjImport.TryImport(s, mode, out Mesh m, out string errMsg))
                throw new InvalidOperationException();

            return m;
        }

        private static Size WpfSizeToPixels(FrameworkElement element)
        {
            var source = PresentationSource.FromVisual(element);
            System.Windows.Media.Matrix transformToDevice = source.CompositionTarget.TransformToDevice;

            return (Size)transformToDevice.Transform(new System.Windows.Vector(element.ActualWidth, element.ActualHeight));
        }

        public void AttachViewModel(Warp9ViewModel vm)
        {

        }

        public void DetachViewModel()
        {

        }
    }
}
