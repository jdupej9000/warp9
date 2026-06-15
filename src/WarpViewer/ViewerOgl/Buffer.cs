using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;
using Warp9.Data;
using Warp9.Utils;

namespace Warp9.ViewerOgl
{
    public enum BufferKind
    {
        Vertex,
        Index
    }

    public class Buffer : IDisposable
    {
        private Buffer(GL gl, BufferKind k, MeshSegmentFormat f, int count, bool dynamic)
        {
            this.gl = gl;
            handle = gl.GenBuffer();

            if (k == BufferKind.Index)
            {
                bufferSize = 12 * count;
            }
            else
            {
                bufferSize = MiscUtils.GetStructElemSize(f) * MiscUtils.GetNumStructElems(f) * count; 
            }

            Kind = k;
            IsDynamic = dynamic;
        }

        GL gl;
        uint handle;
        int bufferSize;
        bool isDataSet = false;
        
        public bool IsDynamic {get; private init; }
        public BufferKind Kind { get; private init; }

        private GLEnum GlKind => Kind switch 
        { 
            BufferKind.Vertex => GLEnum.ArrayBuffer, 
            BufferKind.Index => GLEnum.ElementArrayBuffer,
            _ => throw new InvalidOperationException()
        };

        public void Bind()
        {
            gl.BindBuffer(GlKind, handle);
        }

        public void Unbind()
        {
            gl.BindBuffer(GlKind, 0);
        }

        public void SetData(ReadOnlySpan<byte> data, bool forceDynamicResize = false)
        {
            Bind();

            if (isDataSet && IsDynamic && !forceDynamicResize)
            {
                gl.BufferSubData(GlKind, 0, data);
            }
            else
            {
                gl.BufferData(GlKind, data, IsDynamic ? BufferUsageARB.DynamicDraw : BufferUsageARB.StaticDraw);
            }
            Unbind();

            isDataSet = true;
        }

        public static Buffer CreateVb(GL gl, MeshSegmentFormat fmt, int count, bool dynamic=false)
        {
            return new Buffer(gl, BufferKind.Vertex, fmt, count, dynamic);
        }

        public static Buffer CreateIb(GL gl, int faceCount)
        {
            return new Buffer(gl, BufferKind.Index, MeshSegmentFormat.Unknown, faceCount, false);
        }

        public void Dispose()
        {
            gl.DeleteBuffer(handle);
        }
    }
}
