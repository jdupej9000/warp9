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

        public static DrawCall CreateTriangleList(GL gl, int vertexCount)
        {
            return new DrawCall(gl) { primitiveKind = GLEnum.Triangles, vertexStart = 0, vertexCount = vertexCount};
        }
    }
}

