using Silk.NET.OpenGL;

namespace Warp9.Viewer
{
    public class DrawCall
    {
        public DrawCall(GL gl)
        {
            this.gl = gl;
        }

        GL gl;
        GLEnum primitiveKind;
        int vertexStart, vertexCount;

        public void Execute()
        {
            gl.DrawArrays(primitiveKind, vertexStart, (uint)vertexCount);
        }
    }
}

