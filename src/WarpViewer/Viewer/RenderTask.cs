using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.Viewer
{
    public class RenderTask
    {
        public RenderTask(GL gl)
        {
            this.gl = gl;
        }

        GL gl;
        ShaderProgram? program;    
        Buffer?[] vertexBuffers;
        Buffer? indexBuffer;
        uint vao; // GenVertexArrays
        List<DrawCall> drawCalls = new List<DrawCall>();

        public long Version { get; private set; } = 0;

        public ShaderProgram Program
        {
            get 
            { 
                return program ?? throw new NullReferenceException(); 
            }
            set
            {
                if (program is not null) 
                    program.Dispose(); 
                
                program = value;
            }
        }

        public bool TryUpdate(long newVersion)
        {
            if (newVersion > Version)
            {
                Version = newVersion;
                return true;
            }

            return false;
        }

        public void Execute()
        {
            gl.BindVertexArray(vao);
            indexBuffer?.Bind();
            Program.Bind();

            foreach(DrawCall dc in drawCalls)
                dc.Execute();
            
            gl.BindVertexArray(0);            
        }


        // call this when the buffer layout is changed
        private void UpdateVertexBuffers()
        {
            if(vao == uint.MaxValue)
                vao = gl.GenVertexArray();

            if(vertexBuffers is null)
                return;

            gl.BindVertexArray(vao);

            for(uint i = 0; i < vertexBuffers.Length; i++)
            {
                if(vertexBuffers[i] is not null)
                {
                    gl.EnableVertexAttribArray(i);
                    gl.VertexAttribFormat(i, 3, GLEnum.Float, false, 0);
                    gl.VertexAttribBinding(i, i);
                }
                else
                {
                    gl.DisableVertexAttribArray(i);
                }
            }

        }
    }
}
