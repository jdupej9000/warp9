using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

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

        EstimateNormals = StockShaders.PshConst_Flags_EstimateNormals,

        ShowValueLevel = StockShaders.PshConst_Flags_ValueLevel
    };

    public class RenderItemMesh : RenderItemBase
    {
        public RenderItemMesh()
        {
        }

        Mesh? mesh;
        Lut? lut;
        float[]? valueBuffer;
        float levelValue;
        Color color;
        Matrix4x4? modelMatrix;
        MeshRenderStyle style;
        bool constBuffDirty = true;

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
        public Color Color
        {
            get { return color; }
            set { color = value; constBuffDirty = true; }
        }

        public Matrix4x4? ModelMatrix
        {
            get { return modelMatrix; }
            set { modelMatrix = value; constBuffDirty = true; }
        }

        public void SetValueField(float[] val)
        {
            valueBuffer = val;
            Commit();
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

            MeshView? posView = mesh.GetView(MeshViewKind.Pos3f);
            if (posView is null)
            {
                SetError("Mesh has no vertex position view.");
                return true;
            }

            job.SetVertexBuffer(ctx, 0, posView.RawData, posView.GetLayout(), false);

            if (valueBuffer is not null)
            {
                VertexDataLayout layoutValue = new VertexDataLayout();
                layoutValue.AddTex(SharpDX.DXGI.Format.R32_Float, 1, 0);
                job.SetVertexBuffer(ctx, 1, MemoryMarshal.Cast<float, byte>(valueBuffer.AsSpan()), layoutValue);
            }

            DrawCall dcMain;
            if (mesh.IsIndexed)
            {
                MeshView? indexView = mesh.GetView(MeshViewKind.Indices3i);
                if (indexView is null)
                {
                    SetError("Mesh is indexed but has no index view.");
                    return true;
                }

                job.SetIndexBuffer(ctx, indexView.RawData, SharpDX.DXGI.Format.R32_UInt);
                dcMain = job.SetDrawCall(0, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList,
                    0, mesh.FaceCount * 3);
            }
            else
            {
                dcMain = job.SetDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList,
                    0, mesh.VertexCount);
            }

            if (lut is not null)
                job.SetTexture(ctx, 1, lut, false);

            constBuffDirty = true;

            return true;
        }

        public override void UpdateConstantBuffers(RenderJob job)
        {
            base.UpdateConstantBuffers(job);

            if (constBuffDirty)
            {
                PshConst pshConst = new PshConst();
                pshConst.color = RenderUtils.ToNumColor(color);
                pshConst.valueLevel = levelValue;
                pshConst.flags = (uint)style;
                pshConst.ambStrength = 0.1f;
                job.SetConstBuffer(0, StockShaders.Name_PshConst, pshConst);

                ModelConst mc = new ModelConst();
                if (modelMatrix is not null)
                    mc.model = Matrix4x4.Transpose(modelMatrix.Value);
                else
                    mc.model = Matrix4x4.Identity;
                job.SetConstBuffer(-1, StockShaders.Name_ModelConst, mc);
               
            }
        }

        private void SetError(string err)
        {
            // TODO: disable all drawcalls
        }
    }
}
