using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.ViewerOgl
{
    public class ShaderProgram : IDisposable
    {
        private ShaderProgram(GL gl, uint program)
        {
            this.gl = gl;
            this.program = program;
        }

        GL gl;
        uint program;

        public void Bind()
        {
            gl.UseProgram(program);
        }

        public void Dispose()
        {   
            gl.DeleteProgram(program);
        }

    }
}
