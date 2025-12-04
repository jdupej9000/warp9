using SharpDX.Direct3D11;
using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Warp9.Data;
using Warp9.IO;
using Warp9.Utils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Warp9.Viewer
{
    [Flags]
    public enum MeshRenderStyle : uint
    {
        ColorFlat = StockShaders.PshConst_Flags_ColorFlat,
        ColorArray = StockShaders.PshConst_Flags_ColorArray,
        ColorTex = StockShaders.PshConst_Flags_ColorTex,
        ColorLut = StockShaders.PshConst_Flags_ColorScale,

        PhongBlinn = StockShaders.PshConst_Flags_PhongBlinn,
        DiffuseLighting = StockShaders.PshConst_Flags_DiffuseLighting,

        EstimateNormals = StockShaders.PshConst_Flags_EstimateNormals,

        ShowValueLevel = StockShaders.PshConst_Flags_ValueLevel
    };

    public class RenderItemMesh : RenderItemBase
    {
        public RenderItemMesh(bool autoCommit=true)
        {
            AutoCommit = autoCommit;
        }

        Mesh? mesh;
        Lut? lut;
        bool useDynamicArrays = false;
        BufferSegment<float>? valueBuffer;
        IBufferSegment? posUpdateDyn, normalUpdateDyn;
        int posUpdateDynElemSize = 0, normalUpdateDynElemSize = 0;
        float levelValue, valueMin = 0, valueMax = 1;
        Color fillColor, pointWireColor;
        Matrix4x4 modelMatrix = Matrix4x4.Identity;
        MeshRenderStyle style;
        bool constBuffDirty = true;

        BlendMode renderBlend = BlendMode.Default;
        bool renderPoints = false, renderWire = false, renderFace = true, renderCull = false, renderDepth = true, renderLineSegments = false;

        private static VertexDataLayout FakeNormalsLayout = new VertexDataLayout(false)
            .AddNormal(MeshSegmentFormat.Float32x3, 0);


        public bool RenderLineSegments
        {
            get { return renderLineSegments; }
            set { renderLineSegments = value; constBuffDirty = true; }
        }

        public bool RenderPoints
        {
            get { return renderPoints; }
            set { renderPoints = value; constBuffDirty = true; }
        }

        public bool RenderWireframe
        {
            get { return renderWire; }
            set { renderWire = value; constBuffDirty = true; }
        }

        public bool RenderFace
        {
            get { return renderFace; }
            set { renderFace = value; constBuffDirty = true; }
        }

        public bool RenderCull
        {
            get { return renderCull; }
            set { renderCull = value; constBuffDirty = true; }
        }

        public bool RenderDepth
        {
            get { return renderDepth; }
            set { renderDepth = value; constBuffDirty = true; }
        }

        public BlendMode RenderBlend
        {
            get { return renderBlend; }
            set { renderBlend = value; constBuffDirty = true; }
        }

        public float ValueMin
        {
            get { return valueMin; }
            set { valueMin = value; constBuffDirty = true; }
        }

        public float ValueMax
        {
            get { return valueMax; }
            set { valueMax = value; constBuffDirty = true; }
        }

        public bool UseDynamicArrays
        {
            get { return useDynamicArrays; }
            set { useDynamicArrays = value; Commit(); }
        }

        public Mesh? Mesh
        {
            get { return mesh; }
            set { mesh = value; Commit(); }
        }

        public Lut? Lut
        {
            get { return lut; }
            set { lut = value; Commit(); }
        }

        public float LevelValue
        {
            get { return levelValue; }
            set { levelValue = value; constBuffDirty = true; }
        }

        public MeshRenderStyle Style
        {
            get { return style; }
            set { style = value; constBuffDirty = true; }
        }
        public Color FillColor
        {
            get { return fillColor; }
            set { fillColor = value; constBuffDirty = true; }
        }

        public Color PointWireColor
        {
            get { return pointWireColor; }
            set { pointWireColor = value; constBuffDirty = true; }
        }

        public Matrix4x4 ModelMatrix
        {
            get { return modelMatrix; }
            set { modelMatrix = value; constBuffDirty = true; }
        }

        public void SetValueField(BufferSegment<float> val)
        {
            valueBuffer = val;
            Commit();
        }

        public void UpdateData<T>(BufferSegment<T> data, MeshSegmentSemantic kind) where T: struct
        {
            switch (kind)
            {
                case MeshSegmentSemantic.Position:
                    posUpdateDyn = data;
                    posUpdateDynElemSize = Marshal.SizeOf<T>();
                    break;

                case MeshSegmentSemantic.Normal:
                    normalUpdateDyn = data;
                    normalUpdateDynElemSize = Marshal.SizeOf<T>();
                    break;

                default:
                    throw new NotSupportedException();
            }
           
            Commit(RenderItemDelta.Dynamic);
        }

        protected override void PartialUpdateJobInternal(RenderItemDelta kind, RenderJob job, DeviceContext ctx)
        {
            if (kind.HasFlag(RenderItemDelta.Dynamic))
            {
                // TODO: lock this
                if (posUpdateDyn is not null && posUpdateDyn.Length > 0 && posUpdateDynElemSize != 0)
                {              
                    job.TryUpdateDynamicVertexBuffer(ctx, 0, posUpdateDyn.RawData); // TODO: use SetVertexBuffer instead (this fails if no buffer has been loaded yet)
                }

                if (normalUpdateDyn is not null && normalUpdateDyn.Length > 0 && normalUpdateDynElemSize != 0)
                {
                    job.TryUpdateDynamicVertexBuffer(ctx, 2, normalUpdateDyn.RawData);
                }
            }
        }

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            if (mesh is null)
            {
                SetError("Mesh is null");
                return true;
            }

            job.SetShader(ctx, ShaderType.Vertex, "VsDefault");
            job.SetShader(ctx, ShaderType.Pixel, "PsDefault");

            if (mesh.TryGetRawData(MeshSegmentSemantic.Position, out ReadOnlySpan<byte> posData, out MeshSegmentFormat posFmt))
            {
                VertexDataLayout layout = new VertexDataLayout();
                layout.AddPosition(posFmt, 0);

                if (posUpdateDyn is not null && posUpdateDyn.Length > 0 && posUpdateDynElemSize != 0)
                    job.SetVertexBuffer(ctx, 0,posUpdateDyn.RawData, layout, useDynamicArrays);
                else
                    job.SetVertexBuffer(ctx, 0, posData, layout, useDynamicArrays);
            }
            else
            {
                SetError("Mesh has no vertex position view.");
                return true;
            }

            if (mesh.TryGetRawData(MeshSegmentSemantic.Normal, out ReadOnlySpan<byte> normData, out MeshSegmentFormat normFmt))
            {
                VertexDataLayout layout = new VertexDataLayout();
                layout.AddNormal(posFmt, 0);

                if (useDynamicArrays)
                    job.SetVertexBuffer(ctx, 2, Array.Empty<byte>(), FakeNormalsLayout, useDynamicArrays, posData.Length);
                else
                    job.SetVertexBuffer(ctx, 2, normData, layout, useDynamicArrays);
            }

            if (mesh.TryGetRawData(MeshSegmentSemantic.Color, out ReadOnlySpan<byte> colorData, out MeshSegmentFormat colorFmt))
            {
                VertexDataLayout layout = new VertexDataLayout();
                layout.AddColor(colorFmt, 0, 0);
                job.SetVertexBuffer(ctx, 3, colorData, layout, false);
            }
            
            if (valueBuffer is not null)
            {
                VertexDataLayout layoutValue = new VertexDataLayout();
                layoutValue.AddTex(MeshSegmentFormat.Float32, 1, 0);
                job.SetVertexBuffer(ctx, 1, valueBuffer.RawData, layoutValue);
            }

            SharpDX.Direct3D.PrimitiveTopology topo = renderLineSegments ?
                SharpDX.Direct3D.PrimitiveTopology.LineList :
                SharpDX.Direct3D.PrimitiveTopology.TriangleList;

            DrawCall dcFace, dcWire;
            if (mesh.IsIndexed)
            { 
                if (!mesh.TryGetIndexData(out ReadOnlySpan<FaceIndices> idxData))
                {
                    SetError("Mesh is indexed but has no index view.");
                    return true;
                }

                job.SetIndexBuffer(ctx, MemoryMarshal.Cast<FaceIndices, byte>(idxData), SharpDX.DXGI.Format.R32_UInt);
                dcFace = job.SetDrawCall(0, true, topo, 0, mesh.FaceCount * 3);
                dcWire = job.SetDrawCall(1, true, topo, 0, mesh.FaceCount * 3);
            }
            else
            {
                dcFace = job.SetDrawCall(0, false, topo, 0, mesh.VertexCount);
                dcWire = job.SetDrawCall(1, false, topo, 0, mesh.VertexCount);
            }

            DrawCall dcPoints = job.SetDrawCall(2, false, SharpDX.Direct3D.PrimitiveTopology.PointList,
                0, mesh.VertexCount);

            UpdateDrawCallSettings(job);

            if (lut is not null)
                job.SetTexture(ctx, 1, lut, true);

            constBuffDirty = true;

            return true;
        }

        public override void UpdateConstantBuffers(RenderJob job, IRendererViewport vport)
        {
            base.UpdateConstantBuffers(job, vport);

            if (constBuffDirty)
            {
                PshConst pshConst = new PshConst
                {
                    color = RenderUtils.ToNumColor(fillColor),
                    valueLevel = levelValue,
                    flags = (uint)style,
                    ambStrength = 0.1f,
                    valueMin = valueMin,
                    valueScale = 1.0f / (valueMax - valueMin)
                };
                job.TrySetConstBuffer(0, StockShaders.Name_PshConst, pshConst);

                PshConst pshConstWirePoint = new PshConst
                {
                    color = RenderUtils.ToNumColor(pointWireColor),
                    valueLevel = levelValue,
                    flags = (uint)(MeshRenderStyle.ColorFlat),
                    ambStrength = 0.1f
                };
                job.TrySetConstBuffer(1, StockShaders.Name_PshConst, pshConstWirePoint);
                job.TrySetConstBuffer(2, StockShaders.Name_PshConst, pshConstWirePoint);

                ModelConst mc = new ModelConst();
                mc.model = Matrix4x4.Transpose(modelMatrix);
             
                job.TrySetConstBuffer(-1, StockShaders.Name_ModelConst, mc);

                UpdateDrawCallSettings(job);
                constBuffDirty = false;
            }
        }

        private void UpdateDrawCallSettings(RenderJob job)
        {
            job.TryEnableDrawCall(0, renderFace);
            job.TryEnableDrawCall(1, renderWire);
            job.TryEnableDrawCall(2, renderPoints);

            if (job.TryGetDrawCall(0, out DrawCall? dcFace) && dcFace is not null)
            {
                dcFace.RastMode = RasterizerMode.Solid;
                if (renderCull) dcFace.RastMode |= RasterizerMode.CullBack;

                dcFace.DepthMode = renderDepth ? DepthMode.UseDepth : DepthMode.NoDepth;
                dcFace.BlendMode = renderBlend;
            }

            if (job.TryGetDrawCall(1, out DrawCall? dcWire) && dcWire is not null)
            {
                dcWire.RastMode = RasterizerMode.Wireframe;
                if (renderCull) dcWire.RastMode |= RasterizerMode.CullBack;

                dcWire.DepthMode = renderDepth ? DepthMode.UseDepth : DepthMode.NoDepth;
                dcWire.BlendMode = renderBlend;
            }

            if (job.TryGetDrawCall(2, out DrawCall? dcPoint) && dcPoint is not null)
            {
                dcPoint.RastMode = RasterizerMode.Solid;
                dcPoint.DepthMode = renderDepth ? DepthMode.UseDepth : DepthMode.NoDepth;
                dcPoint.BlendMode = renderBlend;
            }
        }

        private void SetError(string err)
        {
            // TODO: disable all drawcalls
        }
    }
}
