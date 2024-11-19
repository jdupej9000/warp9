using System;
using System.Runtime.InteropServices;
using Warp9.Viewer;

namespace Warp9.Data
{
    public enum MeshViewKind
    {
        Pos3f,
        Normal3f,
        Indices3i
    }

    public class MeshView
    {
        public MeshView(MeshViewKind kind, byte[] data, Type t)
        {
            Kind = kind;
            //Format = MeshUtils.TypeToDxgi[t];
            RawData = data;
            dataType = t;
        }

        public MeshViewKind Kind { get; private init; }
        //public SharpDX.DXGI.Format Format { get; private init; }

        public byte[] RawData { get; private init; }

        private readonly Type dataType;

        public bool AsTypedData<T>(out ReadOnlySpan<T> data) where T : struct
        {
            if (typeof(T) == dataType)
            {
                data = MemoryMarshal.Cast<byte, T>(RawData.AsSpan());
                return true;
            }

            data = default;
            return false;
        }

        public VertexDataLayout GetLayout()
        {
            VertexDataLayout ret = new VertexDataLayout();

            switch (Kind)
            {
                case MeshViewKind.Pos3f:
                    ret.AddPosition(SharpDX.DXGI.Format.R32G32B32_Float, 0);
                    break;

                case MeshViewKind.Normal3f:
                    ret.AddNormal(SharpDX.DXGI.Format.R32G32B32_Float, 0);
                    break;

                case MeshViewKind.Indices3i:
                    break;

                default:
                    throw new InvalidOperationException();
            }

            return ret;
        }
    }
}
