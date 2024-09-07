using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Viewer;

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
        public ShaderSpec()
        {
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

            ShaderSpec ret = new ShaderSpec();
            ret.Name = name;
            ret.Code = code;
            ret.Type = sht;
            ret.ConstantBuffers = constantBuffers.ToArray();
            return ret;
        }

        public static ShaderSpec Create(string name, ShaderType sht, IEnumerable<ConstBuffAssgn> constantBuffers, IEnumerable<SemanticAssgn> semantics, string code)
        {
            if (sht != ShaderType.Vertex)
                throw new NotSupportedException("Only vertex buffers can specify input semantics.");

            ShaderSpec ret = new ShaderSpec();
            ret.Name = name;
            ret.Code = code;
            ret.Type = sht;
            ret.ConstantBuffers = constantBuffers.ToArray();
            ret.Semantics = semantics.ToArray();
            return ret;
        }
    }
}
