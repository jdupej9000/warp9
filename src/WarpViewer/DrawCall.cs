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

        public bool Enabled { get; set; } = true;
        public bool IsIndexed { get; internal set; }
        public PrimitiveTopology Topology { get; internal set; }
        public RasterizerMode RastMode { get; internal set; } = RasterizerMode.Solid | RasterizerMode.CullBack;
        public BlendMode BlendMode { get; internal set; } = BlendMode.Default;
        public DepthMode DepthMode { get; internal set; } = DepthMode.UseDepth;
        public int FirstElem { get; internal set; }
        public int NumElems { get; internal set; }
        public int FirstVertexIdx { get; internal set; }
        public Dictionary<int, ConstantBufferPayload> ConstBuffPayloads => cbuffPayloads;

        public void Execute(DeviceContext ctx, StateCache stateCache)
        {
            if (!Enabled) return;

            ctx.InputAssembler.PrimitiveTopology = Topology;
        
            if (stateCache.RasterizerStateCache.LastState != RastMode)
                ctx.Rasterizer.State = stateCache.RasterizerStateCache.Get(RastMode);

            if (stateCache.BlendStateCache.LastState != BlendMode)
                ctx.OutputMerger.BlendState = stateCache.BlendStateCache.Get(BlendMode);

            if (stateCache.DepthStateCache.LastState != DepthMode)
                ctx.OutputMerger.DepthStencilState = stateCache.DepthStateCache.Get(DepthMode);

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
