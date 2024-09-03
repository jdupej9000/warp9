using SharpDX.Direct3D11;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SharpDX.D3DCompiler;

namespace Warp9.Viewer
{
    internal class RenderJobBuffer : IDisposable
    {
        private RenderJobBuffer(SharpDX.Direct3D11.Buffer buff, int elemSize)
        {
            buffer = buff;
            elementSize = elemSize;
            itemCount = 1;
        }

        private RenderJobBuffer(SharpDX.Direct3D11.Buffer buff, BindFlags bind, SharpDX.DXGI.Format fmt, int cnt, int elemSize, bool dyn)
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
        public SharpDX.Direct3D11.Buffer Buffer => buffer;
        public SharpDX.DXGI.Format Format => format;

        public VertexDataLayout Layout { get; set; }

        public void UpdateConstant(DeviceContext ctx, byte[] payload)
        {
            ctx.UpdateSubresource(payload, buffer);
        }

        internal bool TryUpdateDynamic(DeviceContext ctx, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();

            if (!isDynamic || data.Length > itemCount * elementSize) return false;

            DataBox db = ctx.MapSubresource(buffer, MapMode.WriteDiscard, MapFlags.None, out DataStream ds);
            //.Write(data);
            ctx.UnmapSubresource(buffer, 0);
            Utilities.Dispose(ref ds);

            return false;
        }

        internal static RenderJobBuffer Create(Device device, byte[] data, BindFlags bindFlags, SharpDX.DXGI.Format fmt, int itemCount, int elemSize, bool dyn)
        {
            int dataSize = elemSize * itemCount;

            using DataStream ds = new DataStream(dataSize, true, true);
            ds.Write(data, 0, Math.Min(dataSize, data.Length));
            ds.Position = 0;

            SharpDX.Direct3D11.Buffer buff = new SharpDX.Direct3D11.Buffer(
                device, ds, dataSize,
                dyn ? ResourceUsage.Dynamic : ResourceUsage.Default,
                bindFlags,
                dyn ? CpuAccessFlags.Write : CpuAccessFlags.None,
                ResourceOptionFlags.None, 0); 
            
           return new RenderJobBuffer(buff, bindFlags, fmt, itemCount, elemSize, dyn);
        }

        internal static RenderJobBuffer CreateConstant(Device device, byte[] data)
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

           return new RenderJobBuffer(buff, length);
        }

        public void Dispose()
        {
            Utilities.Dispose(ref buffer);
        }
    }


}
