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

        public void Render()
        {
            gl.ClearColor(CanvasColor);
            gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            gl.Enable(EnableCap.DepthTest);
        }
    }
}
