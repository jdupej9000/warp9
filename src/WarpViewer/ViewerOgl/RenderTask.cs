using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.ViewerOgl
{
    public class RenderTask
    {
        public RenderTask(GL gl)
        {
            this.gl = gl;
        }

        GL gl;
        ShaderProgram? program;
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
            Program.Bind();
        }
    }
}
