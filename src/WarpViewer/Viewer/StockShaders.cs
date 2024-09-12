using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Warp9.Viewer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ModelConst
    {
        public Matrix4x4 model;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ViewProjConst
    {
        public Matrix4x4 viewProj;
        public Vector4 camera;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraLightConst
    {
        public Vector3 lightPos;
        private uint reserved0;
        public Vector3 cameraPos;
        private uint reserved1;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct PshConst
    {
        public Vector4 color;
        public float ambStrength;
        public float valueLevel;
        public uint flags;
        private uint reserved0;
    };

    public static class StockShaders
    {
        public const int Name_ModelConst = 0;
        public const int Name_ViewProjConst = 1;
        public const int Name_CameraLightConst = 2;
        public const int Name_PshConst = 3;

        public const uint PshConst_Flags_ColorFlat = 0;
        public const uint PshConst_Flags_ColorArray = 1;
        public const uint PshConst_Flags_ColorTex = 2;
        public const uint PshConst_Flags_ColorScale = 3;

        public const uint PshConst_Flags_PhongBlinn = 0x10;

        public const uint PshConst_Flags_EstimateNormals = 0x100;
        public const uint PshConst_Flags_ValueLevel = 0x200;

        public readonly static ShaderSpec VsDefault = ShaderSpec.Create(
            "VsDefault", 
            ShaderType.Vertex,
            [   new (0, Name_ModelConst), 
                new (1, Name_ViewProjConst) 
            ],
            [   new ("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float), 
                new ("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float),
                new ("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float),
                new ("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float),
                new ("TEXCOORD", 1, SharpDX.DXGI.Format.R32_Float)
            ], @"
struct VsInput
{
   float3 pos : POSITION;
   float4 color : COLOR0;
   float2 tex0: TEXCOORD0;
   float3 normal : NORMAL;
   float value : TEXCOORD1;
};

struct VsOutput
{
   float4 pos : SV_POSITION;
   float3 posw : POSITION1;
   float4 color : COLOR0;
   float2 tex0 : TEXCOORD0;
   float3 normal : NORMAL;
   float value : TEXCOORD1;
};

cbuffer ModelConst : register(b0)
{
   matrix model;
};

cbuffer ViewProjConst : register(b1)
{
   matrix viewProj;
   float4 camera;
}

VsOutput main(VsInput input)
{
   VsOutput ret;
   float4 posw = mul(float4(input.pos, 1), model);
   ret.posw = posw.xyz;
   ret.pos = mul(posw, viewProj);
   ret.color = input.color;
   ret.tex0 = input.tex0;
   ret.normal = normalize(mul(input.normal, (float3x3)model));
   ret.value = input.value;
   return ret;
}
");

        public readonly static ShaderSpec VsDefaultInstanced = ShaderSpec.Create(
            "VsDefaultInstanced",
            ShaderType.Vertex,
            [   new (0, Name_ModelConst),
                new (1, Name_ViewProjConst)
            ],
            [   new ("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float),
                new ("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float),
                new ("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float),
                new ("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float),
                new ("TEXCOORD", 7, SharpDX.DXGI.Format.R32G32B32_Float)
            ], @"
struct VsInput
{
   float3 pos : POSITION;
   float4 color : COLOR0;
   float2 tex0: TEXCOORD0;
   float3 normal : NORMAL;
   float3 translation : TEXCOORD7;
};

struct VsOutput
{
   float4 pos : SV_POSITION;
   float3 posw : POSITION1;
   float4 color : COLOR0;
   float2 tex0 : TEXCOORD0;
   float3 normal : NORMAL;
   float value : TEXCOORD1;
};

cbuffer ModelConst : register(b0)
{
   matrix model;
};

cbuffer ViewProjConst : register(b1)
{
   matrix viewProj;
   float4 camera;
}

VsOutput main(VsInput input)
{
   VsOutput ret;
   float4 posw = mul(float4(input.pos, 1), model) + float4(input.translation, 0);
   ret.posw = posw.xyz;
   ret.pos = mul(posw, viewProj);
   ret.color = input.color;
   ret.tex0 = input.tex0;
   ret.normal = normalize(mul(input.normal, (float3x3)model));
   ret.value = 0;
   return ret;
}
");

        public readonly static ShaderSpec PsDefault = ShaderSpec.Create(
            "PsDefault", 
            ShaderType.Pixel,
            [   new (0, Name_CameraLightConst), 
                new (1, Name_PshConst)
            ], @"
struct VsOutput
{
   float4 pos : SV_POSITION;
   float3 posw : POSITION1;
   float4 color : COLOR0;
   float2 tex0 : TEXCOORD0;
   float3 normal : NORMAL;
   float value : TEXCOORD1;
};

cbuffer CameraLightConst : register(b0)
{
   float3 lightpos;
   float3 camerapos;
};

cbuffer PshConst : register(b1)
{
   float4 color;
   float ambStrength;
   float valueLevel;
   uint flags;
};

Texture2D tex0 : register(t0);
SamplerState sam0 : register(s0);
Texture1D texScale : register(t1);

float4 phong(float4 amb, float4 diff, float3 normal, float3 posw)
{
   float3 l = normalize(lightpos - posw);
   float3 v = normalize(camerapos - posw);
   float3 h = normalize(l + v);
 
   float cosdiff = dot(normal, l);
   float cosspec = 0;
 
   if(cosdiff > 0) cosspec = pow(dot(h, normal), 40);
   else cosdiff = 0;

   float4 ret = amb + diff * cosdiff + float4(1,1,1,0) * cosspec;
   return saturate(ret);
}

float4 main(VsOutput input) : SV_TARGET
{
   float4 ret = color;
   if((flags & 0xf) == 1) ret = input.color;
   if((flags & 0xf) == 2) ret = tex0.Sample(sam0, input.tex0);
   if((flags & 0xf) == 3) ret = texScale.Sample(sam0, input.value);
 
   float3 normal = input.normal;
   if((flags & 0x100) == 0x100)
      normal = normalize(cross(ddx(input.posw), ddy(input.posw))); // ideally this should be posw-cameraPos calculated in vert shader

   if((flags & 0xf0) == 0x10)
      ret = phong(float4(ambStrength.rrr,1) * ret, ret, normal, input.posw);

   if((flags & 0x200) == 0x200)
   {
      float dlevel = 0.5 * length(float2(ddx(input.value), ddy(input.value)));
      float level = saturate((abs(input.value - valueLevel) - dlevel) / dlevel);
      ret = lerp(color, ret, level);
   }

   return ret;
}
");
    }
}
