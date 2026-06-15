using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Warp9.ViewerOgl
{
    public class Renderer
    {
        public Renderer(GL gl)
        {
            this.gl = gl;
        }

        GL gl;
        public Color CanvasColor { get; set; } = Color.Firebrick;

        readonly Dictionary<RenderItemBase, RenderJob?> renderItems = new Dictionary<RenderItemBase, RenderJob?>();

        public string DeviceInfo => gl.GetStringS(StringName.Renderer);
        public string DeviceVendor => gl.GetStringS(StringName.Vendor);
        public string DeviceVersion => gl.GetStringS(StringName.Version);
        public string DeviceGlslVersion => gl.GetStringS(StringName.ShadingLanguageVersion);

        public void Render()
        {
            PreRender();

            gl.ClearColor(CanvasColor);
            gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            gl.Enable(EnableCap.DepthTest);
        }

        public virtual void Resize(int width, int height)
        {
        }

        protected virtual void PreRender()
        {
        }
    }
}
