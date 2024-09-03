using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace Warp9.Viewer
{
    public class DrawCall
    {
        public DrawCall()
        {
        }

        Dictionary<int, ConstantBufferPayload> cbuffPayloads = new Dictionary<int, ConstantBufferPayload>();

        public bool IsIndexed { get; internal set; }
        public PrimitiveTopology Topology { get; internal set; }
        public int FirstElem { get; internal set; }
        public int NumElems { get; internal set; }
        public int FirstVertexIdx { get; internal set; }
        public Dictionary<int, ConstantBufferPayload> ConstBuffPayloads => cbuffPayloads;

        public void Execute(DeviceContext ctx)
        {
            ctx.InputAssembler.PrimitiveTopology = Topology;
            /*ctx.Rasterizer.State = new RasterizerState(ctx.Device, new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                IsFrontCounterClockwise = true,
                FillMode = FillMode.Solid
            });*/

            if (IsIndexed)
                ctx.DrawIndexed(NumElems, FirstElem, FirstVertexIdx);
            else
                ctx.Draw(NumElems, FirstVertexIdx);
        }

        public static DrawCall CreateIndexed(PrimitiveTopology topo, int first, int num, int offs = 0)
        {
            return new DrawCall { IsIndexed = true, Topology = topo, FirstElem = first, NumElems = num, FirstVertexIdx = offs };
        }

        public static DrawCall Create(PrimitiveTopology topo, int first, int num)
        {
            return new DrawCall { IsIndexed = false, Topology = topo, FirstElem = first, NumElems = num, FirstVertexIdx = 0 };
        }
    }
}
