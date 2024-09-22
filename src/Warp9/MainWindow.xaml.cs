﻿using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Warp9.Controls;
using Warp9.Data;
using Warp9.IO;
using Warp9.Themes;
using Warp9.Viewer;

namespace Warp9
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }


        WpfInteropRenderer? renderer;
        RenderItemMesh? meshRend;
        TimeSpan lastRender = TimeSpan.Zero;
        private Random rnd = new Random();

        #region remove
        public static readonly string AssetsPath = @"../../test/data/";

        public static Stream OpenAsset(string name)
        {
            string path = Path.Combine(AssetsPath, name);

            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }
        public static Mesh LoadObjAsset(string name, ObjImportMode mode)
        {
            using Stream s = OpenAsset(name);
            if (!ObjImport.TryImport(s, mode, out Mesh m, out string errMsg))
                throw new InvalidOperationException();

            return m;
        }
        #endregion

        
        #region WPF-DX interop
      
        private void Grid_Loaded2(object sender, RoutedEventArgs e)
        {
            if (!WpfInteropRenderer.TryCreate(0, out renderer) || renderer is null)
                throw new InvalidOperationException();

            renderer.CanvasColor = System.Drawing.Color.Black;
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

            InteropImage.WindowOwner = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            InteropImage.IsFrontBufferAvailableChanged += InteropImage_IsFrontBufferAvailableChanged;
            InteropImage.OnRender += OnRender;

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

      

        void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;
            
            // It's possible for Rendering to call back twice in the same frame 
            // so only render when we haven't already rendered in this frame.
            // Also, limit to 60 fps
            if ((args.RenderingTime - lastRender).TotalSeconds >= 1.0 )
            {
                if(meshRend is not null)
                    meshRend.Color = System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));

                InteropImage.RequestRender();
                lastRender = args.RenderingTime;
            }
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

        void OnRender(IntPtr resourcePtr, bool isNewSurface)
        {
            if(renderer is null)
                throw new InvalidOperationException();

            if (isNewSurface)
            {
                // a new surface has been created (e.g. after a resize)
                renderer.EnsureSharedBackBuffer(resourcePtr, WpfSizeToPixels(ImageGrid));
            }

            renderer.Present();
        }

        
        #endregion

        

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size size = WpfSizeToPixels(ImageGrid);
            InteropImage.SetPixelSize((int)size.Width, (int)size.Height);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Grid_Loaded2(sender, e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            // TODO: renderer dispose
        }

        private static Size WpfSizeToPixels(FrameworkElement element)
        {
            var source = PresentationSource.FromVisual(element);
            Matrix transformToDevice = source.CompositionTarget.TransformToDevice;

            return (Size)transformToDevice.Transform(new System.Windows.Vector(element.ActualWidth, element.ActualHeight));
        }

      

        private void mnuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow wnd = new AboutWindow();
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}