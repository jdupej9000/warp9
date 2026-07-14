using System;
using System.Collections.Generic;
using System.Text;
using Silk.NET.GLFW;

namespace Warp9.Viewer
{
    public enum ShaderKind
    {
        Vertex,
        Pixel,
        Geometry
    }

    public record ShaderSpec
    {
        public ShaderSpec(ShaderKind kind, (DataType, string)[] input, (DataType, string)[] output, (DataType, string)[] uniform, string code)
        {
            Kind = kind;
            Input = input;
            Output = output;
            Uniform = uniform;
            ShaderCode = code;
        }

        public ShaderKind Kind {get; init;}
        public (DataType, string)[] Input {get; init;}
        public (DataType, string)[] Output {get; init;}
        public (DataType, string)[] Uniform {get; init;}
        public string ShaderCode {get; init;}

        static readonly Dictionary<DataType, string> DataTypeString = new Dictionary<DataType, string>()
        {
            {DataType.Float, "float"},
            {DataType.Float2, "vec2"},
            {DataType.Float3, "vec3"},
            {DataType.Float4, "vec4"},
            {DataType.Float4x4, "mat4"},
            {DataType.Int, "int"},
        };

        public string GetFullShaderCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#version 330 core");
            foreach(var x in Input)
                sb.AppendLine($"in {DataTypeString[x.Item1]} {x.Item2};");

            foreach(var x in Output)
                sb.AppendLine($"out {DataTypeString[x.Item1]} {x.Item2};");

            foreach(var x in Uniform)
                sb.AppendLine($"uniform {DataTypeString[x.Item1]} {x.Item2};");

            sb.AppendLine();
            sb.AppendLine(ShaderCode);

            return sb.ToString();
        }
    }
}