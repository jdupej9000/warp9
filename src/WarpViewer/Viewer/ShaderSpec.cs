using System;
using System.Collections.Generic;
using System.Text;
using Silk.NET.GLFW;

namespace Warp9.Viewer
{
    public enum ShaderDataType
    {
        Float,
        Float2,
        Float3,
        Float4,
        Float4x4,
        Int
    }

    public enum ShaderKind
    {
        Vertex,
        Pixel,
        Geometry
    }

    public record ShaderSpec
    {
        public ShaderSpec(ShaderKind kind, (ShaderDataType, string)[] input, (ShaderDataType, string)[] output, (ShaderDataType, string)[] uniform, string code)
        {
            Kind = kind;
            Input = input;
            Output = output;
            Uniform = uniform;
            ShaderCode = code;
        }

        public ShaderKind Kind {get; init;}
        public (ShaderDataType, string)[] Input {get; init;}
        public (ShaderDataType, string)[] Output {get; init;}
        public (ShaderDataType, string)[] Uniform {get; init;}
        public string ShaderCode {get; init;}

        static readonly Dictionary<ShaderDataType, string> DataTypeString = new Dictionary<ShaderDataType, string>()
        {
            {ShaderDataType.Float, "float"},
            {ShaderDataType.Float2, "vec2"},
            {ShaderDataType.Float3, "vec3"},
            {ShaderDataType.Float4, "vec4"},
            {ShaderDataType.Float4x4, "mat4"},
            {ShaderDataType.Int, "int"},
        };

        public string GetFullShaderCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#version 330 core");
            foreach(var x in Input)
                sb.AppendLine($"in {DataTypeString[x.Item1]} {x.Item2}");

            foreach(var x in Output)
                sb.AppendLine($"out {DataTypeString[x.Item1]} {x.Item2}");

            foreach(var x in Uniform)
                sb.AppendLine($"uniform {DataTypeString[x.Item1]} {x.Item2}");

            sb.AppendLine();
            sb.AppendLine(ShaderCode);

            return sb.ToString();
        }
    }
}