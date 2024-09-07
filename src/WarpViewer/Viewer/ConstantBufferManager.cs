﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace Warp9.Viewer
{
    public class ConstantBufferManager
    {
        public ConstantBufferManager() 
        {
        }

        Dictionary<int, Buffer> constBuffers = new Dictionary<int, Buffer>();

        public void Set(DeviceContext ctx, int idx, ConstantBufferPayload payload)
        {
            System.Console.WriteLine(string.Format("Updating buff #{0}, length={1}",
                idx, payload.StructSize));

            if (!constBuffers.TryGetValue(idx, out Buffer? buffer))
                constBuffers[idx] = Buffer.CreateConstant(ctx.Device, payload.RawData);
            else
                buffer.UpdateConstant(ctx, payload.RawData);
        }

        internal Buffer Get(int idx)
        {
            return constBuffers[idx];
        }

    }
}