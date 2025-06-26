using SharpDX;
using SharpDX.Direct3D11;
using System;

namespace Warp9.Viewer
{
    internal class Buffer : IDisposable
    {
        private Buffer(SharpDX.Direct3D11.Buffer buff, int elemSize)
        {
            buffer = buff;
            elementSize = elemSize;
            itemCount = 1;
        }

        private Buffer(SharpDX.Direct3D11.Buffer buff, BindFlags bind, SharpDX.DXGI.Format fmt, int cnt, int elemSize, bool dyn)
        {
            buffer = buff;
            bindFlags = bind;
            format = fmt;
            itemCount = cnt;
            elementSize = elemSize;
            isDynamic = dyn;

            if(bindFlags == BindFlags.VertexBuffer)
                binding = new VertexBufferBinding(buffer, elemSize, 0);
        }

        SharpDX.Direct3D11.Buffer buffer;
        VertexBufferBinding binding;
        BindFlags bindFlags;
        SharpDX.DXGI.Format format;
        int itemCount;
        int elementSize;
        bool isDynamic;

        public VertexBufferBinding Binding => binding;
        public SharpDX.Direct3D11.Buffer NativeBuffer => buffer;
        public SharpDX.DXGI.Format Format => format;

        public VertexDataLayout? Layout { get; set; }

        public void UpdateConstant(DeviceContext ctx, byte[] payload)
        {
            ctx.UpdateSubresource(payload, buffer);
        }

        internal bool TryUpdateDynamic(DeviceContext ctx, ReadOnlySpan<byte> data)
        {
            if (!isDynamic || data.Length > itemCount * elementSize) return false;

            ctx.MapSubresource(buffer, MapMode.WriteDiscard, MapFlags.None, out DataStream ds);
            (ds as System.IO.Stream).Write(data);
            ctx.UnmapSubresource(buffer, 0);
            Utilities.Dispose(ref ds);

            return false;
        }

        internal static Buffer Create(Device device, ReadOnlySpan<byte> d, BindFlags bindFlags, SharpDX.DXGI.Format fmt, int itemCount, int elemSize, bool dyn)
        {
            int dataSize = elemSize * itemCount;

            using DataStream ds = new DataStream(dataSize, true, true);
            if(!d.IsEmpty)
                (ds as System.IO.Stream).Write(d);

            ds.Position = 0;

            SharpDX.Direct3D11.Buffer buff = new SharpDX.Direct3D11.Buffer(
                device, ds, dataSize,
                dyn ? ResourceUsage.Dynamic : ResourceUsage.Default,
                bindFlags,
                dyn ? CpuAccessFlags.Write : CpuAccessFlags.None,
                ResourceOptionFlags.None, 0); 
            
           return new Buffer(buff, bindFlags, fmt, itemCount, elemSize, dyn);
        }

        internal static Buffer CreateConstant(Device device, byte[] data)
        {
            int length = data.Length;

            using DataStream ds = new DataStream(length, true, true);
         
            ds.WriteRange(data);
            ds.Position = 0;

            SharpDX.Direct3D11.Buffer buff = new SharpDX.Direct3D11.Buffer(
                device, ds, length,
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None, 0);

           return new Buffer(buff, length);
        }

        public void Dispose()
        {
            Utilities.Dispose(ref buffer);
        }
    }


}
