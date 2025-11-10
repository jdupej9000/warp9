using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.IO;

namespace Warp9.Viewer
{
    internal class Shader : IDisposable
    {
        internal Shader(ShaderSpec spec, CompilationResult result, ShaderSignature signature)
        {
            Spec = spec;
            CompilationResult = result;
            Signature = signature;
        }

        public ShaderSpec Spec { get; internal set; }
        public CompilationResult CompilationResult { get; internal set; }
        public ShaderSignature Signature { get; internal set; }
        public DeviceChild? ShaderObject { get; set; }

        public void Dispose()
        {
            ShaderSignature ss = Signature;
            Utilities.Dispose(ref ss);

            if (ShaderObject is not null)
            {
                DeviceChild dc = ShaderObject;
                Utilities.Dispose(ref dc);
            }
        }
    }

    public class ShaderRegistry : Include, IDisposable
    {
        readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

        public void AddShaders(params IEnumerable<ShaderSpec> specs)
        {
            foreach (ShaderSpec spec in specs)
            {
                AddShader(spec);
            }
        }

        public void AddShader(ShaderSpec spec)
        {
            if (shaders.ContainsKey(spec.Name))
                throw new InvalidOperationException();

            string profile = spec.Type switch
            {
                ShaderType.Vertex => "vs_5_0",
                ShaderType.Pixel => "ps_5_0",
                ShaderType.Geometry => "gs_5_0",
                _ => throw new InvalidOperationException()
            };

            // This throws on failures. Do not catch. All errors are contract violations.
            CompilationResult result = ShaderBytecode.Compile(spec.Code, "main", profile,
                ShaderFlags.None, EffectFlags.None,
                Array.Empty<SharpDX.Direct3D.ShaderMacro>(), this);

            if (result.HasErrors)
                throw new InvalidOperationException(result.Message);

            ShaderSignature signature = ShaderSignature.GetInputSignature(result.Bytecode);
            shaders[spec.Name] = new Shader(spec, result, signature);
        }

#pragma warning disable CS8618
        public IDisposable Shadow { get; set; }
#pragma warning restore CS8618

        public ShaderSignature GetShaderSignature(string name)
        {
            if (shaders.TryGetValue(name, out Shader? sh))
            {
                return sh.Signature;
            }

            throw new InvalidOperationException();
        }

        public PixelShader GetPixelShader(Device device, string name)
        {
            if (shaders.TryGetValue(name, out Shader? sh) && sh.Spec.Type == ShaderType.Pixel)
            {
                if (sh.ShaderObject == null)
                    sh.ShaderObject = new PixelShader(device, sh.CompilationResult.Bytecode);

                if(sh.ShaderObject is PixelShader psh)
                    return psh;
            }

            throw new InvalidOperationException();
        }

        public VertexShader GetVertexShader(Device device, string name)
        {
            if (shaders.TryGetValue(name, out Shader? sh) && sh.Spec.Type == ShaderType.Vertex)
            {
                if (sh.ShaderObject == null)
                    sh.ShaderObject = new VertexShader(device, sh.CompilationResult.Bytecode);

                if (sh.ShaderObject is VertexShader vsh)
                    return vsh;
            }

            throw new InvalidOperationException();
        }

        public GeometryShader GetGeometryShader(Device device, string name)
        {
            if (shaders.TryGetValue(name, out Shader? sh) && sh.Spec.Type == ShaderType.Geometry)
            {
                if (sh.ShaderObject == null)
                    sh.ShaderObject = new GeometryShader(device, sh.CompilationResult.Bytecode);

                if (sh.ShaderObject is GeometryShader gsh)
                    return gsh;
            }
            
            throw new InvalidOperationException();
        }

        public ConstBuffAssgn[] GetConstBufferAssignment(string name)
        {
            if (shaders.TryGetValue(name, out Shader? sh))
                return sh.Spec.ConstantBuffers;

            throw new InvalidOperationException();
        }

        public SemanticAssgn[] GetInputSemanticAssignment(string name)
        {
            if (shaders.TryGetValue(name, out Shader? sh) && sh.Spec.Type == ShaderType.Vertex)
                return sh.Spec.Semantics!;

            throw new InvalidOperationException();
        }

        public void Close(Stream stream)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            foreach (var sh in shaders)
                sh.Value.Dispose();

            shaders.Clear();
        }

        public Stream Open(IncludeType type, string fileName, Stream parentStream)
        {
            // we do not support #includes at the moment
            throw new NotImplementedException();
        }
    }
}
