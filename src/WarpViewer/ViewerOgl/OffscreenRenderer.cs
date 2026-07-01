using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.ViewerOgl
{
    public class OffscreenRenderer : Renderer, IDisposable
    {
        private OffscreenRenderer(GL gl, IWindow wnd) : base(gl) 
        { 
            window = wnd;
            fbo = new Fbo(gl);
        }

        IWindow window;
        Fbo fbo;

        public override void Resize(int width, int height)
        {
            fbo.Resize(width, height);
        }

        public void ExtractColor(Span<byte> dest)
        {
            if (dest.Length < fbo.ColorBitmapSizeBytes)
                throw new InvalidOperationException();

            fbo.Read(dest, true);
        }

        public void Dispose()
        {
            fbo.Dispose();
            window.Dispose();
        }

        protected override void PreRender()
        {
            fbo.Bind();
        }

        public static OffscreenRenderer Create()
        {
            (GL gl, IWindow wnd) = CreateOffscreenContextAndWindow(4, 6);
            return new OffscreenRenderer(gl, wnd);
        }

        private static (GL, IWindow) CreateOffscreenContextAndWindow(int apiMajor = 4, int apiMinor = 5)
        {
            WindowOptions options = WindowOptions.Default with
            {
                Size = new(256, 256),
                Title = "Offscreen",
                IsVisible = false,
                ShouldSwapAutomatically = false,
                API = new GraphicsAPI(
                    ContextAPI.OpenGL, 
                    ContextProfile.Core,
                    ContextFlags.ForwardCompatible,
                    new APIVersion(apiMajor, apiMinor))
            };

            IWindow window = Window.Create(options);
            window.Initialize();

            return (GL.GetApi(window), window);
        }      
    }
}
