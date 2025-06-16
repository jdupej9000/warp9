using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Warp9.Data;

namespace Warp9.Viewer
{
    public enum RenderJobExecuteStatus
    {
        Ok = 0,
        InvalidResources = 1,
        InvalidVertexLayout = 2
    }

    public class RenderJob
    {
        public RenderJob(ShaderRegistry shaders, ConstantBufferManager cbuffs)
        {
            shaderRegistry = shaders;
            constantBuffersManager = cbuffs;
        }

        uint itemVersion = 0;

        ShaderSignature? shaderSignatureVert;
        PixelShader? shaderPix;
        VertexShader? shaderVert;
        GeometryShader? shaderGeom;
        ConstBuffAssgn[]? cbuffShaderPix;
        ConstBuffAssgn[]? cbuffShaderVert;
        ConstBuffAssgn[]? cbuffShaderGeom;
        SemanticAssgn[]? inputSemanticAssgn;
        Dictionary<int, ConstantBufferPayload> constantBuffers = new Dictionary<int, ConstantBufferPayload>();
        Dictionary<int, Texture> textures = new Dictionary<int, Texture>();
        ConstantBufferManager constantBuffersManager;
        ShaderRegistry shaderRegistry;

        InputLayout? inputLayout;
        readonly Dictionary<int, DrawCall> drawCalls = new Dictionary<int, DrawCall>();
        readonly Dictionary<int, Buffer> vertBuffBindings = new Dictionary<int, Buffer>();
        Buffer? indexBuffer;

        bool rebuildInputLayout = false;

        public bool NeedsUpdate(uint masterItemVersion)
        {
            return itemVersion != masterItemVersion;
        }

        public void CommitVersion(uint masterItemVersion)
        {
            itemVersion = masterItemVersion;
        }

        public bool TrySetConstBuffer<T>(int drawCallId, int buffId, T value) where T : struct
        {
            if (drawCallId < 0)
            {
                if (constantBuffers.TryGetValue(buffId, out ConstantBufferPayload? payload))
                    payload.Set<T>(value);
                else
                    constantBuffers.Add(buffId, new ConstantBufferPayload<T>(value));
            }
            else if (drawCalls.TryGetValue(drawCallId, out DrawCall? drawCall))
            {
                if (drawCall.ConstBuffPayloads.TryGetValue(buffId, out ConstantBufferPayload? payload))
                    payload.Set<T>(value);
                else
                    drawCall.ConstBuffPayloads.Add(buffId, new ConstantBufferPayload<T>(value));
            }
            else
            {
                return false;
            }

            return true;
        }

        public RenderJobExecuteStatus Render(DeviceContext ctx, StateCache stateCache)
        {
            if (shaderVert is null || shaderPix is null || shaderSignatureVert is null || inputSemanticAssgn is null)
                return RenderJobExecuteStatus.InvalidResources;

            EnsureInputLayout(ctx, shaderSignatureVert, inputSemanticAssgn);
            if (inputLayout is null)
                return RenderJobExecuteStatus.InvalidVertexLayout;

            ctx.InputAssembler.InputLayout = inputLayout;

            foreach (var vbb in vertBuffBindings)
                ctx.InputAssembler.SetVertexBuffers(vbb.Key, vbb.Value.Binding);
           
            if (indexBuffer is not null)
                ctx.InputAssembler.SetIndexBuffer(indexBuffer.NativeBuffer, indexBuffer.Format, 0);

            ctx.VertexShader.Set(shaderVert);
            ctx.PixelShader.Set(shaderPix);
            if (shaderGeom is not null)
                ctx.GeometryShader.Set(shaderGeom);
            else
                ctx.GeometryShader.Set(null);

            ApplyConstBuffPayloads(ctx, constantBuffers);

            foreach (var tex in textures)
            {
                ctx.PixelShader.SetShaderResources(tex.Key, tex.Value.ResourceView);
                ctx.PixelShader.SetSampler(tex.Key, stateCache.SamplerStateCache.Get(SamplerMode.Linear));
            }

            foreach (DrawCall dc in drawCalls.Values)
            {
                if (dc.ConstBuffPayloads.Count > 0)
                    ApplyConstBuffPayloads(ctx, dc.ConstBuffPayloads);

                dc.Execute(ctx, stateCache);
            }

            return RenderJobExecuteStatus.Ok;
        }

        private void ApplyConstBuffPayloads(DeviceContext ctx, Dictionary<int, ConstantBufferPayload> cbuffs)
        {
            foreach (var kvp in cbuffs)
                constantBuffersManager.Set(ctx, kvp.Key, kvp.Value);

            if (cbuffShaderVert is not null)
            {
                foreach (ConstBuffAssgn cba in cbuffShaderVert)
                    ctx.VertexShader.SetConstantBuffer(cba.Slot, constantBuffersManager.Get(cba.BufferName).NativeBuffer);
            }

            if (cbuffShaderPix is not null)
            {
                foreach (ConstBuffAssgn cba in cbuffShaderPix)
                    ctx.PixelShader.SetConstantBuffer(cba.Slot, constantBuffersManager.Get(cba.BufferName).NativeBuffer);
            }

            if (cbuffShaderGeom is not null && shaderGeom is not null)
            {
                foreach (ConstBuffAssgn cba in cbuffShaderGeom)
                    ctx.GeometryShader.SetConstantBuffer(cba.Slot, constantBuffersManager.Get(cba.BufferName).NativeBuffer);
            }
        }

        public void ClearDrawCalls()
        {
            drawCalls.Clear();
        }

        public void RemoveDrawCall(int slot)
        {
            drawCalls.Remove(slot);
        }

        public DrawCall SetDrawCall(int slot, bool indexed, PrimitiveTopology topo, int firstElem, int numElems, int vertexOffset = 0)
        {
            DrawCall dc;
            if (indexed)
                dc = DrawCall.CreateIndexed(topo, firstElem, numElems, vertexOffset);
            else
                dc = DrawCall.Create(topo, firstElem, numElems);

            drawCalls[slot] = dc;
            return dc;
        }

        public bool TryGetDrawCall(int slot, out DrawCall? dc)
        {
            return drawCalls.TryGetValue(slot, out dc);
        }

        public DrawCall SetInstancedDrawCall(int slot, bool indexed, PrimitiveTopology topo, int firstInst, int numInst, int firstElem, int numElems)
        {
            if(firstElem != 0) 
                throw new NotSupportedException();

            DrawCall dc;
            if (indexed)
                dc = DrawCall.CreateIndexedInstanced(topo, firstInst, numInst, numElems);
            else
                dc = DrawCall.CreateInstanced(topo, firstInst, numInst, numElems);

            drawCalls[slot] = dc;
            return dc;
        }

        public bool TryEnableDrawCall(int slot, bool enable)
        {
            if (drawCalls.TryGetValue(slot, out DrawCall? dc))
            {
                dc.Enabled = enable;
            }
            else
            {
                return false;
            }

            return true;
        }

        public void ClearVertexBuffers()
        {
            foreach (Buffer rjb in vertBuffBindings.Values)
                rjb.Dispose();

            vertBuffBindings.Clear();
            rebuildInputLayout = true;
        }

        public void RemoveVertexBuffer(int slot)
        {
            if (vertBuffBindings.TryGetValue(slot, out Buffer? rjb) && rjb is not null)
            {
                rjb.Dispose();
                vertBuffBindings.Remove(slot);

                rebuildInputLayout = true;
            }
        }

        public void SetVertexBuffer(DeviceContext ctx, int slot, ReadOnlySpan<byte> data, VertexDataLayout layout, bool isDynamic = false)
        {
            if (vertBuffBindings.TryGetValue(slot, out Buffer? rjb) && rjb is not null)
            {
                if (rjb.TryUpdateDynamic(ctx, data))
                    return;
                else
                    rjb.Dispose();
            }

            int vertexStructSize = layout.StrideBytes;

            vertBuffBindings[slot] = Buffer.Create(ctx.Device, data, 
                BindFlags.VertexBuffer, 
                SharpDX.DXGI.Format.R8_UInt,
                data.Length / vertexStructSize, vertexStructSize, isDynamic);

            vertBuffBindings[slot].Layout = layout;

            rebuildInputLayout = true;
        }

        public void SetIndexBuffer(DeviceContext ctx, ReadOnlySpan<byte> data, SharpDX.DXGI.Format format)
        {
            int elemSize = RenderUtils.GetStructSizeBytes(format);
            indexBuffer = Buffer.Create(ctx.Device, data,
                BindFlags.IndexBuffer,
                format,
                data.Length / elemSize, elemSize, false);
        }

        public void SetTexture(DeviceContext ctx, int slot, Bitmap bmp, bool isDynamic = false)
        {
            if (textures.TryGetValue(slot, out Texture? tex) && tex is not null)
            {
                if (tex.TryUpdateDynamic(ctx, bmp))
                    return;
                else
                    tex.Dispose();
            }

            textures[slot] = Texture.Create(ctx.Device, bmp, isDynamic);
        }

        public void SetTexture(DeviceContext ctx, int slot, Lut lut, bool isDynamic = false)
        {
            if (textures.TryGetValue(slot, out Texture? tex) && tex is not null)
            {
                if (tex.TryUpdateDynamic(ctx, lut))
                    return;
                else
                    tex.Dispose();
            }

            textures[slot] = Texture.Create(ctx.Device, lut, isDynamic);
        }

        public void SetShader(DeviceContext ctx, ShaderType sht, string name)
        {
            if (sht == ShaderType.Vertex)
            {
                shaderVert = shaderRegistry.GetVertexShader(ctx.Device, name);
                shaderSignatureVert = shaderRegistry.GetShaderSignature(name);
                cbuffShaderVert = shaderRegistry.GetConstBufferAssignment(name);
                inputSemanticAssgn = shaderRegistry.GetInputSemanticAssignment(name);
                rebuildInputLayout = true;
            }
            else if (sht == ShaderType.Pixel)
            {
                shaderPix = shaderRegistry.GetPixelShader(ctx.Device, name);
                cbuffShaderPix = shaderRegistry.GetConstBufferAssignment(name);
            }
            else if (sht == ShaderType.Geometry)
            {
                shaderGeom = shaderRegistry.GetGeometryShader(ctx.Device, name);
                cbuffShaderGeom = shaderRegistry.GetConstBufferAssignment(name);
            }
        }

        public void UnsetGeometryShader()
        { 
            shaderGeom = null; 
        }

        protected void EnsureInputLayout(DeviceContext ctx, ShaderSignature shaderSignatureVert, SemanticAssgn[] semantics)
        {
            if (rebuildInputLayout || inputLayout is null)
            {
                if (inputLayout is not null)
                    Utilities.Dispose(ref inputLayout);

                List<InputElement> listInputElems = new List<InputElement>();
                if (vertBuffBindings.Count == 1)
                {
                    listInputElems.AddRange(vertBuffBindings[0]?.Layout?.ToArray() ?? 
                        throw new InvalidOperationException("Input layout must have a slot 0 assigned."));
                }
                else
                {
                    foreach (var kvp in vertBuffBindings)
                    {
                        if (kvp.Value.Layout is null)
                            throw new InvalidOperationException();

                        kvp.Value.Layout.AddToGrandLayout(listInputElems, kvp.Key);
                    }
                }

                foreach (SemanticAssgn sem in semantics)
                {
                    if (!listInputElems.Any((ie) => ie.SemanticName == sem.Semantic && ie.SemanticIndex == sem.Slot))
                        listInputElems.Add(new InputElement(sem.Semantic, sem.Slot, sem.Format, 0, 0)); 
                }
               
                inputLayout = new InputLayout(ctx.Device, shaderSignatureVert, listInputElems.ToArray());
                rebuildInputLayout = false;
            }
        }
    }
}
