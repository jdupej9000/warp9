using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

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

        readonly Dictionary<RenderItemBase, RenderTask?> renderItems = new Dictionary<RenderItemBase, RenderTask?>();

        public string DeviceInfo => gl.GetStringS(StringName.Renderer);
        public string DeviceVendor => gl.GetStringS(StringName.Vendor);
        public string DeviceVersion => gl.GetStringS(StringName.Version);
        public string DeviceGlslVersion => gl.GetStringS(StringName.ShadingLanguageVersion);

        public void Render()
        {
            List<(RenderItemBase, RenderTask)> updates = new List<(RenderItemBase, RenderTask)>();
            foreach (var kvp in renderItems)
            {
                if (kvp.Value is null)
                {
                    RenderTask task = new RenderTask(gl);
                    kvp.Key.ProjectToTask(task);
                    updates.Add((kvp.Key, task));
                }
                else if (kvp.Key.ProjectToTask(kvp.Value))
                {
                    updates.Add((kvp.Key, kvp.Value));
                }
                else
                {
                    // no update
                }
            }

            foreach((RenderItemBase rib, RenderTask rt) in updates)
                renderItems[rib] = rt;

            PreRender();

            gl.ClearColor(CanvasColor);
            gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            gl.Enable(EnableCap.DepthTest);

            foreach (var kvp in renderItems)
                kvp.Value?.Execute();

            PostRender();
        }

        public virtual void Resize(int width, int height)
        {
        }

        protected virtual void PreRender()
        {
        }

        protected virtual void PostRender()
        {
        }
    }
}
