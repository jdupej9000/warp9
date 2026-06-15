using Silk.NET.OpenGL;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Warp9.ViewerOgl
{
    public class Fbo : IDisposable
    {
        public Fbo(GL gl)
        {
            this.gl = gl;

            texColor = gl.GenTexture();
            rboDepth = gl.GenRenderbuffer();
            fbo = gl.GenFramebuffer();
        }

        GL gl;
        uint texColor, rboDepth, fbo;
        int fboWidth, fboHeight;

        public int ColorBitmapSizeBytes => fboWidth * fboHeight * 4;

        public void Resize(int width, int height)
        {
            gl.BindTexture(TextureTarget.Texture2D, texColor);
            unsafe
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0,
                    InternalFormat.Rgba8,
                    (uint)width, (uint)height, 0,
                    GLEnum.Rgba, PixelType.UnsignedByte, null);
            }

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.BindTexture(TextureTarget.Texture2D, 0);
                  
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                InternalFormat.DepthComponent24, (uint)width, (uint)height);
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, texColor, 0);

            gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, rboDepth);

            GLEnum status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
                throw new Exception($"FBO incomplete: {status}");

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            fboWidth = width;
            fboHeight = height;
        }

        public void Bind()
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        }

        public void Unbind()
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Read(Span<byte> destination, bool flip=true)
        {
            Bind();
            gl.PixelStore(GLEnum.PackAlignment, 1);
            gl.ReadPixels(0, 0, (uint)fboWidth, (uint)fboHeight, PixelFormat.Bgra, PixelType.UnsignedByte, destination);
            Unbind();

            if (flip)
            {
                int stride = fboWidth * 4;

                byte[] t = ArrayPool<byte>.Shared.Rent(stride);
                for(int y = 0; y < fboHeight / 2; y++)
                {
                    Span<byte> rowUp = destination.Slice(y * stride, stride);
                    Span<byte> rowDown = destination.Slice((fboHeight - y - 1) * stride, stride);

                    rowUp.CopyTo(t);
                    rowDown.CopyTo(rowUp);
                    t.CopyTo(rowDown);
                }

                ArrayPool<byte>.Shared.Return(t);
            }
        }

        public void Dispose()
        {
            gl.DeleteFramebuffer(fbo);
            gl.DeleteTexture(texColor);
            gl.DeleteRenderbuffer(rboDepth);
        }

        
    }
}
