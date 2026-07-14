using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Warp9.Viewer
{
    public class RenderTask
    {
        public RenderTask(GL gl)
        {
            this.gl = gl;
        }

        const int MaxVertexBuffers = 8;

        GL gl;
        ShaderProgram? program;    
        Buffer?[] vertexBuffers = new Buffer?[MaxVertexBuffers];
        Buffer? indexBuffer;
        uint vao; // GenVertexArrays
        List<DrawCall> drawCalls = new List<DrawCall>();
        List<VertexDataMapping> vbuffMappings = new List<VertexDataMapping>();

        public GL GL => gl;
        public long Version { get; private set; } = 0;
        public List<DrawCall> DrawCalls => drawCalls;
        

        public ShaderProgram? Program
        {
            get 
            { 
                return program;
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

        public void SetIndexBuffer(Buffer b)
        {
            indexBuffer = b;
        }

        public bool TryGetIndexBuffer(out Buffer? b)
        {
            if(indexBuffer is not null)
            {
                b = indexBuffer;
                return true;
            }

            b = null;
            return false;
        }

        public void SetVertexBuffer(int slot, Buffer b)
        {
            if(slot < 0 || slot >= MaxVertexBuffers)
                throw new IndexOutOfRangeException();

            if(vertexBuffers[slot] is not null)    
                vertexBuffers[slot]!.Dispose();

            vertexBuffers[slot] = b;
        }

        public bool TryGetVertexBuffer(int slot, out Buffer? b)
        {
            if(slot < 0 || slot >= MaxVertexBuffers)
            {
                b = null;
                return false;
            }

            b = vertexBuffers[slot];
            return b is not null;
        }

        // call this when the buffer layout is changed
        public void UpdateVertexBuffers()
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
                    vertexBuffers[i]!.Bind((int)i);

                    foreach(VertexDataMapping vdm in vbuffMappings)
                    {
                        if(vdm.Slot == (int)i && 
                            program is not null &&
                            program.TryFindAttrib(vdm.AttribName, out DataType shtype, out int handle))
                        {
                            var dt = ViewerUtils.DataTypeToGLEnumAndCount(vdm.DataType);
                            gl.VertexAttribFormat((uint)handle, dt.Count, dt.Kind, false, (uint)vdm.Offset);
                            gl.VertexAttribBinding((uint)handle, i);
                        }
                    }
                }
                else
                {
                    gl.DisableVertexAttribArray(i);
                }
            }

        }
    }
}
