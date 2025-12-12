using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

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
        public float valueMin, valueScale;
        private uint reserved0, reserved1, reserved2;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceConst
    {
        public Vector3 normalRef;
        public float scale;
        public uint flags; // bit0 = rotate by normal from normal_ref, bit1 = use instance color
        uint res0, res1, res2;
    };

    public static class StockShaders
    {
        public const int Name_ModelConst = 0;
        public const int Name_ViewProjConst = 1;
        public const int Name_CameraLightConst = 2;
        public const int Name_PshConst = 3;
        public const int Name_InstanceConst = 4;

        public const uint PshConst_Flags_ColorFlat = 0;
        public const uint PshConst_Flags_ColorArray = 1;
        public const uint PshConst_Flags_ColorTex = 2;
        public const uint PshConst_Flags_ColorScale = 3;

        public const uint PshConst_Flags_PhongBlinn = 0x10;
        public const uint PshConst_Flags_DiffuseLighting = 0x20;

        public const uint PshConst_Flags_EstimateNormals = 0x100;
        public const uint PshConst_Flags_ValueLevel = 0x200;

        public const uint InstanceConst_Flags_RotateByNormal = 0x1;
        public const uint InstanceConst_Flags_UseInstColor = 0x2;

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

        public readonly static ShaderSpec VsText = ShaderSpec.Create(
            "VsText",
            ShaderType.Vertex,
            [   new (1, Name_ViewProjConst)
            ],
            [   new ("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float),
                new ("COLOR", 0, SharpDX.DXGI.Format.R8G8B8A8_UNorm),                
                new ("TEXCOORD", 6, SharpDX.DXGI.Format.R32G32B32A32_Float),
                new ("TEXCOORD", 7, SharpDX.DXGI.Format.R32G32B32A32_Float)
            ], @"
struct VsInput
{
   float2 char_rect : POSITION;
   float4 color: COLOR0;
   float4 screen_rect : TEXCOORD6;
   float4 tex_rect : TEXCOORD7;
};

struct VsOutput
{
   float4 pos : SV_POSITION;
   float3 posw : POSITION1;
   float4 color : COLOR0;
   float2 tex0 : TEXCOORD0;
};

cbuffer ViewProjConst : register(b1)
{
   matrix viewProj;
   float4 camera;
}

VsOutput main(VsInput input)
{
   VsOutput ret;
   float4 posw = float4(input.screen_rect.xy + input.char_rect * input.screen_rect.zw, 0, 1);
   ret.posw = posw.xyz;
   ret.pos = mul(posw, viewProj);
   ret.color = input.color;
   ret.tex0 = input.tex_rect.xy + input.char_rect * input.tex_rect.zw;
   return ret;
}
");

        public readonly static ShaderSpec VsDefaultInstanced = ShaderSpec.Create(
     "VsDefaultInstanced",
     ShaderType.Vertex,
     [  new (0, Name_ModelConst),
        new (1, Name_ViewProjConst),
        new (2, Name_InstanceConst)
     ],
     [  new ("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float),
        new ("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float),
        new ("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float),
        new ("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float),
        new ("TEXCOORD", 5, SharpDX.DXGI.Format.R32G32B32_Float), // instance color
        new ("TEXCOORD", 6, SharpDX.DXGI.Format.R32G32B32_Float), // instance normal
        new ("TEXCOORD", 7, SharpDX.DXGI.Format.R32G32B32_Float)  // instance offset
     ], @"
struct VsInput
{
   float3 pos : POSITION;
   float4 color : COLOR0;
   float2 tex0: TEXCOORD0;
   float3 normal : NORMAL;
   float3 inst_color : TEXCOORD5;
   float3 inst_normal : TEXCOORD6;
   float3 inst_offset : TEXCOORD7;
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

cbuffer InstanceConst : register(b2)
{
    float3 normal_ref;
    float scale;
    uint flags; // bit0 = rotate by normal from normal_ref, bit1 = use instance color
    uint res0, res1, res2;    
};

cbuffer ViewProjConst : register(b1)
{
   matrix viewProj;
   float4 camera;
};

cbuffer ModelConst : register(b0)
{
   matrix model;
};

matrix rotation_from_vectors(float3 from, float3 to)
{
    float3 v = cross(from, to);
    float c = dot(from, to);
    float k = 1.0f / (1.0f + c);
    
    if (c < -0.99999f)
    {        
        float3 orthogonal = abs(from.x) < 0.9f ? float3(1, 0, 0) : float3(0, 1, 0);
        float3 axis = normalize(cross(from, orthogonal));
        return matrix(
            2.0f * axis.x * axis.x - 1.0f,    2.0f * axis.x * axis.y,           2.0f * axis.x * axis.z,        0,
            2.0f * axis.y * axis.x,           2.0f * axis.y * axis.y - 1.0f,    2.0f * axis.y * axis.z,        0,
            2.0f * axis.z * axis.x,           2.0f * axis.z * axis.y,           2.0f * axis.z * axis.z - 1.0f, 0,
            0, 0, 0, 1
        );
    }
    
    // Rodrigues' formula
    return matrix(
        v.x * v.x * k + c,     v.y * v.x * k - v.z,   v.z * v.x * k + v.y, 0,
        v.x * v.y * k + v.z,   v.y * v.y * k + c,     v.z * v.y * k - v.x, 0,
        v.x * v.z * k - v.y,   v.y * v.z * k + v.x,   v.z * v.z * k + c,   0,
        0, 0, 0, 1
    );
}

VsOutput main(VsInput input)
{
   float4 posw = float4(input.pos * scale, 1);
   
   matrix instModel = matrix(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);
   if(flags & 0x1) {
        instModel = transpose(rotation_from_vectors(normal_ref, input.inst_normal));
        posw = mul(posw, instModel);
   }

   posw = mul(posw + float4(input.inst_offset, 0), model);

   VsOutput ret;   
   ret.posw = posw.xyz;
   ret.pos = mul(posw, viewProj);
   ret.color = (flags & 0x2) ? float4(input.inst_color,1) : input.color;
   ret.tex0 = input.tex0;
   ret.normal = normalize(mul(input.normal, (float3x3)mul(instModel,model)));
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
   nointerpolation float4 color : COLOR0;
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
   float valueMin, valueScale;
};

Texture2D tex0 : register(t0);
SamplerState sam0 : register(s0);
Texture1D texScale : register(t1);

float4 phong(float4 amb, float4 diff, float3 normal, float3 posw, float spec)
{
   float3 l = normalize(lightpos - posw);
   float3 v = normalize(camerapos - posw);
   float3 h = normalize(l + v);
   float4 pl = lit(dot(normal, l), dot(normal, h), 40);
   float4 ret = pl.x * amb + pl.y * diff + spec * pl.z * float4(1,1,1,0);
   return saturate(ret);
}

float4 main(VsOutput input) : SV_TARGET
{
   float val = (input.value - valueMin) * valueScale;
   float4 ret = color;
   if((flags & 0xf) == 1) ret = input.color;
   if((flags & 0xf) == 2) ret = tex0.Sample(sam0, input.tex0);
   if((flags & 0xf) == 3) ret = texScale.Sample(sam0, val);
 
   float3 normal = input.normal;
   if((flags & 0x100) == 0x100)
      normal = normalize(cross(ddx(input.posw), ddy(input.posw))); // ideally this should be posw-cameraPos calculated in vert shader

   if((flags & 0xf0) == 0x10)
      ret = phong(float4(ambStrength.rrr,1) * ret, ret, normal, input.posw, 1);
   
   if((flags & 0xf0) == 0x20)
      ret = phong(float4(ambStrength.rrr,1) * ret, ret, normal, input.posw, 0);

   if((flags & 0x200) == 0x200)
   {
      float dlevel = 0.5 * valueScale * length(float2(ddx(input.value), ddy(input.value)));
      float valueLevelNorm = (valueLevel - valueMin) * valueScale;
      float level = saturate((abs(val - valueLevelNorm) - dlevel) / dlevel);
      ret = lerp(color, ret, level);
   }
   //ret.a = color.a;
   return ret;
}
");
    

      public readonly static ShaderSpec PsText = ShaderSpec.Create(
            "PsText",
            ShaderType.Pixel,
            [  
            ], @"
struct VsOutput
{
   float4 pos : SV_POSITION;
   float3 posw : POSITION1;
   float4 color : COLOR0;
   float2 tex : TEXCOORD0;
};

Texture2D tex0 : register(t0);
SamplerState sam0 : register(s0);

float4 main(VsOutput input) : SV_TARGET
{
   float2 dx = 0.33 * ddx(input.tex);
   float2 dy = 0.33 * ddy(input.tex);

   float d =
    step(tex0.Sample(sam0, input.tex - dx - dy).a, 0.5) + step(tex0.Sample(sam0, input.tex - dy).a, 0.5) + step(tex0.Sample(sam0, input.tex + dx - dy).a, 0.5) +
    step(tex0.Sample(sam0, input.tex - dx).a, 0.5) + step(tex0.Sample(sam0, input.tex).a, 0.5) + step(tex0.Sample(sam0, input.tex + dx).a, 0.5) +
    step(tex0.Sample(sam0, input.tex - dx + dy).a, 0.5) + step(tex0.Sample(sam0, input.tex + dy).a, 0.5) + step(tex0.Sample(sam0, input.tex + dx + dy).a, 0.5);

   float4 ret = input.color.bgra;
   ret.a = 1 - d/9;
    
   return ret;
}
");

        public readonly static List<ShaderSpec> AllShaders = [
           VsDefault, VsDefaultInstanced, VsText, PsDefault, PsText
           ];
    }
}
