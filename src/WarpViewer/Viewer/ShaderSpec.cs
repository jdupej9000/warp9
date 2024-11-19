using System;
using System.Collections.Generic;
using System.Linq;

namespace Warp9.Viewer
{
    public struct ConstBuffAssgn(int slot, int buff)
    {
        public int Slot = slot;
        public int BufferName = buff;
    }

    public struct SemanticAssgn(string semantic, int slot, SharpDX.DXGI.Format fmt)
    {
        public string Semantic = semantic;
        public int Slot = slot;
        public SharpDX.DXGI.Format Format = fmt;
    }

    public enum ShaderType
    {
        Vertex,
        Pixel,
        Geometry
    }

    public class ShaderSpec
    {
        private ShaderSpec(string name, ConstBuffAssgn[] cba, string code, ShaderType type)
        {
            Name = name;
            Code = code;
            Type = type;
            ConstantBuffers = cba;
        }

        public string Name;
        public ConstBuffAssgn[] ConstantBuffers { get; private set; }
        public string Code {get; private set;}
        public ShaderType Type {get; private set;}
        public SemanticAssgn[]? Semantics {get; private set;}


        public static ShaderSpec Create(string name, ShaderType sht, IEnumerable<ConstBuffAssgn> constantBuffers, string code)
        {
            if (sht == ShaderType.Vertex)
                throw new NotSupportedException("Vertex buffers must specify input semantics.");

            ShaderSpec ret = new ShaderSpec(name, constantBuffers.ToArray(), code, sht);
            return ret;
        }

        public static ShaderSpec Create(string name, ShaderType sht, IEnumerable<ConstBuffAssgn> constantBuffers, IEnumerable<SemanticAssgn> semantics, string code)
        {
            if (sht != ShaderType.Vertex)
                throw new NotSupportedException("Only vertex buffers can specify input semantics.");

            ShaderSpec ret = new ShaderSpec(name, constantBuffers.ToArray(), code, sht);
            ret.Semantics = semantics.ToArray();
            return ret;
        }
    }
}
