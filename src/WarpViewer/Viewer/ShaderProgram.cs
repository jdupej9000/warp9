using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Warp9.Viewer
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
        Dictionary<string, (DataType, int)> uniforms = new  Dictionary<string, (DataType, int)>();
        Dictionary<string, (DataType, int)> attribs = new  Dictionary<string, (DataType, int)>();

        public void Bind()
        {
            gl.UseProgram(program);
        }

        public void Dispose()
        {   
            gl.DeleteProgram(program);
        }

        private void DiscoverUniforms(ShaderSpec spec)
        {
            foreach(var uniform in spec.Uniform)
            {
                int loc = gl.GetUniformLocation(program, uniform.Item2);
                uniforms[uniform.Item2] = (uniform.Item1, loc);
            }
        }
        private void DiscoverAttribs(ShaderSpec spec)
        {
            foreach(var attr in spec.Input)
            {
                int loc = gl.GetAttribLocation(program, attr.Item2);
                attribs[attr.Item2] = (attr.Item1, loc);
            }
        }

        public bool TryFindAttrib(string name, out DataType t, out int handle)
        {
            if(attribs.TryGetValue(name, out var p))
            {
                t = p.Item1;
                handle = p.Item2;
                return true;
            }

            t = DataType.Invalid;
            handle = -1;
            return false;
        }

        public static ShaderProgram Create(GL gl, ShaderSpec vs, ShaderSpec ps, ShaderSpec? gs = null)
        {
            if(vs.Kind != ShaderKind.Vertex || ps.Kind != ShaderKind.Pixel)
                throw new InvalidOperationException();

            if(gs is not null && gs.Kind != ShaderKind.Geometry)
                throw new InvalidOperationException();

            uint hvs = CompileShader(gl, vs);
            uint hfs = CompileShader(gl, ps);

            uint hgs = uint.MaxValue;
            if(gs is not null)
                hgs = CompileShader(gl, gs);

            uint program = gl.CreateProgram();
            gl.AttachShader(program, hvs);
            gl.AttachShader(program, hfs);
            if(gs is not null)
                gl.AttachShader(program, hgs);

            gl.LinkProgram(program);
            int linkStatus = gl.GetProgram(program, GLEnum.LinkStatus);
            if(linkStatus == 0)
            {
                string log = gl.GetProgramInfoLog(program);
                throw new InvalidOperationException("Program failed to link: " + log);
            }
            
            gl.DeleteShader(hvs);
            gl.DeleteShader(hfs);
            if(gs is not null)
                gl.DeleteShader(hgs);

            ShaderProgram ret = new ShaderProgram(gl, program);
            ret.DiscoverUniforms(vs);
            ret.DiscoverAttribs(vs);
            ret.DiscoverUniforms(ps);
            if(gs is not null)
                ret.DiscoverUniforms(gs);

            return ret;
        }

        public static uint CompileShader(GL gl, ShaderSpec sh)
        {
            uint handle = gl.CreateShader(sh.Kind switch
            {
                ShaderKind.Vertex => GLEnum.VertexShader,
                ShaderKind.Pixel => GLEnum.FragmentShader,
                ShaderKind.Geometry => GLEnum.GeometryShader,
                _ => throw new NotSupportedException($"Shader kind '{sh.Kind}' is not supported.")
            });

            gl.ShaderSource(handle, sh.GetFullShaderCode());
            gl.CompileShader(handle);

            int compilationStatus = gl.GetShader(handle, GLEnum.CompileStatus);
            if(compilationStatus == 0)
            {
                string log = gl.GetShaderInfoLog(handle);
                gl.DeleteShader(handle);
                throw new InvalidOperationException("Shader failed to compile: " + log + "\nFull code:\n" + sh.GetFullShaderCode());
            }

            return handle;
        }
    }
}
