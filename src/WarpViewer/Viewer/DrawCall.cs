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
        public bool IsIndexed { get; set; }
        public bool IsInstanced { get; set; } = false;
        public PrimitiveTopology Topology { get; set; }
        public RasterizerMode RastMode { get; set; } = RasterizerMode.Solid | RasterizerMode.CullBack;
        public BlendMode BlendMode { get; set; } = BlendMode.Default;
        public DepthMode DepthMode { get; set; } = DepthMode.UseDepth;
        public int FirstElem { get; set; }
        public int NumElems { get; set; }
        public int FirstInstance { get; set; } = 0;
        public int NumInstances { get; set; } = 0;
        public int FirstVertexIdx { get; set; }
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

            if (IsInstanced)
            {
                if (IsIndexed)
                    ctx.DrawIndexedInstanced(NumElems, NumInstances, 0, FirstVertexIdx, FirstInstance);
                else
                    ctx.DrawInstanced(NumElems, NumInstances, FirstVertexIdx, FirstInstance);
            }
            else
            {
                if (IsIndexed)
                    ctx.DrawIndexed(NumElems, FirstElem, FirstVertexIdx);
                else
                    ctx.Draw(NumElems, FirstVertexIdx);
            }
        }

        public static DrawCall CreateIndexed(PrimitiveTopology topo, int first, int num, int offs = 0)
        {
            return new DrawCall { IsIndexed = true, Topology = topo, FirstElem = first, NumElems = num, FirstVertexIdx = offs };
        }

        public static DrawCall Create(PrimitiveTopology topo, int first, int num)
        {
            return new DrawCall { IsIndexed = false, Topology = topo, FirstElem = first, NumElems = num, FirstVertexIdx = 0 };
        }

        public static DrawCall CreateInstanced(PrimitiveTopology topo, int firstInst, int numInst, int numElems)
        {
            return new DrawCall
            {
                IsIndexed = false,
                IsInstanced = true,
                Topology = topo,
                FirstElem = 0,
                NumElems = numElems,
                FirstVertexIdx = 0,
                FirstInstance = firstInst,
                NumInstances = numInst
            };
        }

        public static DrawCall CreateIndexedInstanced(PrimitiveTopology topo, int firstInst, int numInst, int numElems)
        {
            return new DrawCall
            {
                IsIndexed = true,
                IsInstanced = true,
                Topology = topo,
                FirstElem = 0,
                NumElems = numElems,
                FirstVertexIdx = 0,
                FirstInstance = firstInst,
                NumInstances = numInst
            };
        }
    }
}
