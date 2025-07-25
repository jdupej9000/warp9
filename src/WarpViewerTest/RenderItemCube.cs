﻿using SharpDX.Direct3D11;
using System.Drawing;
using System.Text.Json.Serialization;
using Warp9.Data;
using Warp9.Viewer;

namespace Warp9.Test
{
    public enum CubeRenderStyle
    {
        NoFillExploded,
        FlatColor,
        ColorArray,
        Texture,
        FlatColorPhong,
        Scale,
        FlatColorPhongEstNormals,
        ScalePhongEstNormals
    };

    public class RenderItemCube : RenderItemBase
    {
        public RenderItemCube()
        {
            Commit();
        }

        static float[] InstanceBuffer = {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            -0.5f, 0.5f, 0.0f,
            0.5f, 0.5f, 0.0f
        };

        static float[] VertexBuffer = {
          //POSITION            COLOR       TEX0  VALUE
            1.0f, -1.0f, -1.0f, 0, 0, 0, 1, 1, 0, 0.1f,
            1.0f, -1.0f, 1.0f,  0, 0, 1, 1, 1, 0, 0.7f,
            -1.0f, -1.0f, 1.0f, 0, 1, 0, 1, 0, 0, 0.2f,
            -1.0f, -1.0f, -1.0f,0, 1, 1, 1, 0, 0, 0.0f,

            1.0f, 1.0f, -1.0f,  1, 0, 0, 1, 1, 1, 0.8f,
            1.0f, 1.0f, 1.0f,   1, 0, 1, 1, 1, 1, 1.0f,
            -1.0f, 1.0f, 1.0f,  1, 1, 0, 1, 0, 1, 0.4f,
            -1.0f, 1.0f, -1.0f, 1, 1, 1, 1, 0, 1, 0.6f
        };
        static int[] IndexBuffer = {
            1,2,3, 7,6,5,
            4,5,1, 5,6,2,
            2,6,7, 0,3,7,
            0,1,3, 4,7,5,
            0,4,1, 1,5,2,
            3,2,7, 4,0,7
        };

        static float[] VertexBufferTriSoup = {
            1,-1,1,0,-1,0,
            -1,-1,1,0,-1,0,
            -1,-1,-1,0,-1,0,
            -1,1,-1,0,1,0,
            -1,1,1,0,1,0,
            1,1,1,0,1,0,
            1,1,-1,1,0,0,
            1,1,1,1,0,0,
            1,-1,1,1,0,0,
            1,1,1,0,0,1,
            -1,1,1,0,0,1,
            -1,-1,1,0,0,1,
            -1,-1,1,-1,0,0,
            -1,1,1,-1,0,0,
            -1,1,-1,-1,0,0,
            1,-1,-1,0,0,-1,
            -1,-1,-1,0,0,-1,
            -1,1,-1,0,0,-1,
            1,-1,-1,0,-1,0,
            1,-1,1,0,-1,0,
            -1,-1,-1,0,-1,0,
            1,1,-1,0,1,0,
            -1,1,-1,0,1,0,
            1,1,1,0,1,0,
            1,-1,-1,1,0,0,
            1,1,-1,1,0,0,
            1,-1,1,1,0,0,
            1,-1,1,0,0,1,
            1,1,1,0,0,1,
            -1,-1,1,0,0,1,
            -1,-1,-1,-1,0,0,
            -1,-1,1,-1,0,0,
            -1,1,-1,-1,0,0,
            1,1,-1,0,0,-1,
            1,-1,-1,0,0,-1,
            -1,1,-1,0,0,-1
        };

        public readonly static ShaderSpec GsExplode = ShaderSpec.Create(
           "GsExplode",
           ShaderType.Geometry,
           [], @"
struct GsInput
{
   float4 pos : SV_POSITION;
   float3 posw : POSITION1;
   float4 color : COLOR0;
   float2 tex0 : TEXCOORD0;
   float3 normal : NORMAL;
   float value : TEXCOORD1;
};

struct GsOutput
{
   float4 pos : SV_POSITION;
   float3 posw : POSITION1;
   float4 color : COLOR0;
   float2 tex0 : TEXCOORD0;
   float3 normal : NORMAL;
   float value : TEXCOORD1;
};

[maxvertexcount(3)]
void main(triangle GsInput input[3], inout TriangleStream<GsOutput> outStream)
{
    GsOutput output;
    
    float3 tri_norm = normalize(cross((input[1].pos - input[0].pos).xyz, (input[2].pos - input[0].pos).xyz));

    [unroll(3)]
    for (int i = 0; i < 3; ++i)
    {
        output.pos = input[i].pos + float4(tri_norm, 0);
        output.posw = input[i].posw;
        output.color = input[i].color;
        output.tex0 = input[i].tex0;
        output.normal = input[i].normal;
        output.value = input[i].value;
        outStream.Append(output);
    }
}
");

        private CubeRenderStyle style = CubeRenderStyle.FlatColor;
        private Color color = Color.Green;
        private bool wireframe = false, instances = false, valueNotch = false, alphaBlend = false;
        private bool buffDirty = true;
        private float valueLevel = 0.5f, valueMin = 0, valueMax = 1;
        private Lut lut = Lut.Create(256, Lut.FastColors);


        public CubeRenderStyle Style
        {
            get { return style; }
            set { style = value; Commit(); }
        }

        public Color Color
        {
            get { return color; }
            set { color = value; buffDirty = true; }
        }

        public bool AddWireframe
        {
            get { return wireframe; }
            set { wireframe = value; buffDirty = true; }
        }

        public bool AddValueLevel
        {
            get { return valueNotch; }
            set { valueNotch = value; buffDirty = true; }
        }

        public float ValueLevel
        {
            get { return valueLevel; }
            set { valueLevel = value; buffDirty = true; }
        }

        public float ValueMin
        {
            get { return valueMin; }
            set { valueMin = value; buffDirty = true; }
        }

        public float ValueMax
        {
            get { return valueMax; }
            set { valueMax = value; buffDirty = true; }
        }

        public bool UseInstances
        {
            get { return instances; }
            set { instances = value; Commit(); }
        }

        public bool AlphaBlend
        {
            get { return alphaBlend; }
            set { alphaBlend = value; Commit(); }
        }

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            bool isTriangleSoup = style == CubeRenderStyle.FlatColorPhong;

            job.SetShader(ctx, ShaderType.Vertex, instances ? "VsDefaultInstanced" : "VsDefault");
            job.SetShader(ctx, ShaderType.Pixel, "PsDefault");

            if(style == CubeRenderStyle.NoFillExploded)
                job.SetShader(ctx, ShaderType.Geometry, "GsExplode");

            if (isTriangleSoup)
            {
                VertexDataLayout layout = new VertexDataLayout();
                layout.AddPosition(SharpDX.DXGI.Format.R32G32B32_Float, 0)
                    .AddNormal(SharpDX.DXGI.Format.R32G32B32_Float, 12);
                job.SetVertexBuffer(ctx, 0, RenderUtils.ToByteArray<float>(VertexBufferTriSoup), layout);
            }
            else
            {
                VertexDataLayout layout = new VertexDataLayout();
                layout.AddPosition(SharpDX.DXGI.Format.R32G32B32_Float, 0)
                    .AddColor(SharpDX.DXGI.Format.R32G32B32A32_Float, 0, 12)
                    .AddTex(SharpDX.DXGI.Format.R32G32_Float, 0, 28)
                    .AddTex(SharpDX.DXGI.Format.R32_Float, 1, 36);
                job.SetVertexBuffer(ctx, 0, RenderUtils.ToByteArray<float>(VertexBuffer), layout);
                job.SetIndexBuffer(ctx, RenderUtils.ToByteArray<int>(IndexBuffer), SharpDX.DXGI.Format.R32_UInt);
            }

            if (instances)
            {
                VertexDataLayout layoutInst = new VertexDataLayout(true);
                layoutInst.AddTex(SharpDX.DXGI.Format.R32G32B32_Float, 7, 0);
                job.SetVertexBuffer(ctx, 1, RenderUtils.ToByteArray<float>(InstanceBuffer), layoutInst);
            }

            DrawCall dcMain;
            DrawCall dcWire;
            int numInst = InstanceBuffer.Length / 3;

            if (isTriangleSoup)
            {
                int numVert = VertexBufferTriSoup.Length / 6;

                if (instances)
                {
                    dcMain = job.SetInstancedDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, numVert);
                    dcWire = job.SetInstancedDrawCall(1, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, numVert);
                }
                else
                {
                    dcMain = job.SetDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numVert);
                    dcWire = job.SetDrawCall(1, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numVert);
                }
            }
            else
            {
                if (instances)
                {
                    dcMain = job.SetInstancedDrawCall(0, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, IndexBuffer.Length);
                    dcWire = job.SetInstancedDrawCall(1, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, IndexBuffer.Length);
                }
                else
                {
                    dcMain = job.SetDrawCall(0, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, IndexBuffer.Length, 0);
                    dcWire = job.SetDrawCall(1, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, IndexBuffer.Length, 0);
                }
            }

            if (Style == CubeRenderStyle.Texture)
            {
                job.SetTexture(ctx, 0, new Bitmap(@"..\..\test\data\_lena256-rgb.png"));
            }
            else if (Style == CubeRenderStyle.Scale || Style == CubeRenderStyle.ScalePhongEstNormals)
            {
                job.SetTexture(ctx, 1, lut);
            }

            dcMain.DepthMode = alphaBlend ? DepthMode.NoDepth : DepthMode.UseDepth;
            dcMain.RastMode = RasterizerMode.NoCull;
            dcMain.BlendMode = alphaBlend ? BlendMode.AlphaBlend : BlendMode.Default;

            if (style == CubeRenderStyle.NoFillExploded)
            {
                dcWire.RastMode = RasterizerMode.Wireframe;
                dcWire.DepthMode = DepthMode.NoDepth;
            }
            else
            {
                dcWire.RastMode = RasterizerMode.Wireframe | RasterizerMode.CullBack;
                dcWire.DepthMode = DepthMode.NoDepth;
            }

            PshConst pshConstWire = new PshConst();
            pshConstWire.flags = StockShaders.PshConst_Flags_ColorFlat;
            pshConstWire.color = RenderUtils.ToNumColor(Color.White);
            job.TrySetConstBuffer(1, StockShaders.Name_PshConst, pshConstWire);

            buffDirty = true;

            return true;
        }

        public override void UpdateConstantBuffers(RenderJob job)
        {
            base.UpdateConstantBuffers(job);

            if (buffDirty)
            {
                PshConst pshConst = new PshConst();
                pshConst.color = RenderUtils.ToNumColor(color);
                pshConst.valueLevel = valueLevel;
                pshConst.valueMin = valueMin;
                pshConst.valueScale = 1.0f / (valueMax - valueMin);

                switch (style)
                {
                    case CubeRenderStyle.NoFillExploded:
                    case CubeRenderStyle.FlatColor:
                        pshConst.flags = StockShaders.PshConst_Flags_ColorFlat;
                        break;

                    case CubeRenderStyle.ColorArray:
                        pshConst.flags = StockShaders.PshConst_Flags_ColorArray;
                        break;

                    case CubeRenderStyle.Texture:
                        pshConst.flags = StockShaders.PshConst_Flags_ColorTex;
                        break;

                    case CubeRenderStyle.FlatColorPhong:
                        pshConst.flags = StockShaders.PshConst_Flags_ColorFlat | StockShaders.PshConst_Flags_PhongBlinn;
                        pshConst.ambStrength = 0.1f;
                        break;

                    case CubeRenderStyle.Scale:
                        pshConst.flags = StockShaders.PshConst_Flags_ColorScale;
                        break;

                    case CubeRenderStyle.FlatColorPhongEstNormals:
                        pshConst.flags = StockShaders.PshConst_Flags_ColorFlat | StockShaders.PshConst_Flags_PhongBlinn | StockShaders.PshConst_Flags_EstimateNormals;
                        break;

                    case CubeRenderStyle.ScalePhongEstNormals:
                        pshConst.flags = StockShaders.PshConst_Flags_ColorScale | StockShaders.PshConst_Flags_PhongBlinn | StockShaders.PshConst_Flags_EstimateNormals;
                        pshConst.ambStrength = 0.5f;
                        break;
                }

                if (valueNotch)
                    pshConst.flags |= StockShaders.PshConst_Flags_ValueLevel;

                job.TrySetConstBuffer(0, StockShaders.Name_PshConst, pshConst);
                job.TryEnableDrawCall(0, style != CubeRenderStyle.NoFillExploded);
                job.TryEnableDrawCall(1, wireframe);

                buffDirty = false;
            }
        }

    }
}
