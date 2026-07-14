using System;
using System.Drawing;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Silk.NET.OpenGL;
using Warp9.ViewerOgl;

namespace Warp9.Avalonia.Controls
{
    public class OglControl : OpenGlControlBase
    {
        //private GL Gl;
        public Renderer? Renderer { get; private set; }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);
            Renderer = new Renderer(GL.GetApi(gl.GetProcAddress));
            Renderer.CanvasColor = Color.FromName("#191A1B");
            //Instantiating our new abstractions
            //Ebo = new BufferObject<uint>(Gl, Indices, BufferTargetARB.ElementArrayBuffer);
            //Vbo = new BufferObject<float>(Gl, Vertices, BufferTargetARB.ArrayBuffer);
            //Vao = new VertexArrayObject<float, uint>(Gl, Vbo, Ebo);

            //Telling the VAO object how to lay out the attribute pointers
            //Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 7, 0);
            //Vao.VertexAttributePointer(1, 4, VertexAttribPointerType.Float, 7, 3);

            //Shader = new Shader(Gl, "shader.vert", "shader.frag");

        }


        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            //Vbo.Dispose();
            //Ebo.Dispose();
            //Vao.Dispose();
            //Shader.Dispose();
            base.OnOpenGlDeinit(gl);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            Renderer?.Render();
            /*Ebo.Bind();
            Vbo.Bind();
            Vao.Bind();
            Shader.Use();
            Shader.SetUniform("uBlue", (float) Math.Sin(DateTime.Now.Millisecond / 1000f * Math.PI));

            Gl.DrawElements(PrimitiveType.Triangles, (uint) Indices.Length, DrawElementsType.UnsignedInt, null);*/
            Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Background);
        }
    }
}