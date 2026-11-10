using System;
using Silk.NET.OpenGL;

namespace Warp9.Viewer
{
    public static class ViewerUtils
    {
        public static GLEnum DataTypeToGLEnum(DataType dt)
        {
            return dt switch
            {
                DataType.Float => GLEnum.Float,
                DataType.Float2 => GLEnum.FloatVec2,
                DataType.Float3 => GLEnum.FloatVec3,
                DataType.Float4 => GLEnum.FloatVec4,
                DataType.Float4x4 => GLEnum.FloatMat4,
                DataType.Int => GLEnum.Int,
                _ => throw new NotSupportedException()
            };
        }

        public static (GLEnum Kind, int Count) DataTypeToGLEnumAndCount(DataType dt)
        {
            return dt switch
            {
                DataType.Float => (GLEnum.Float, 1),
                DataType.Float2 => (GLEnum.Float, 2),
                DataType.Float3 => (GLEnum.Float, 3),
                DataType.Float4 => (GLEnum.Float, 4),
                DataType.Float4x4 => (GLEnum.Float, 16),
                DataType.Int => (GLEnum.Int, 1),
                _ => throw new NotSupportedException()
            };
        }
    }
}