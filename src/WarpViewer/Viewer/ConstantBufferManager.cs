using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;

namespace Warp9.Viewer
{
    public class ConstantBufferManager: IDisposable
    {
        public ConstantBufferManager() 
        {
        }

        Dictionary<int, Buffer> constBuffers = new Dictionary<int, Buffer>();

        public void Set(DeviceContext ctx, int idx, ConstantBufferPayload payload)
        {
#if DEBUG
            System.Console.WriteLine(string.Format("Updating buff #{0}, length={1}",
                idx, payload.StructSize));
#endif

            if (!constBuffers.TryGetValue(idx, out Buffer? buffer))
                constBuffers[idx] = Buffer.CreateConstant(ctx.Device, payload.RawData);
            else
                buffer.UpdateConstant(ctx, payload.RawData);
        }

        internal Buffer Get(int idx)
        {
            return constBuffers[idx];
        }

        public void Dispose()
        {
            foreach (var buffer in constBuffers.Values) 
                buffer.Dispose();
        }
    }
}
