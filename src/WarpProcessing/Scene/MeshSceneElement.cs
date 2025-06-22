using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Model;
using Warp9.Processing;
using Warp9.Viewer;

namespace Warp9.Scene
{
    [Flags]
    public enum MeshRenderFlags : int
    {
        None = 0,
        Wireframe = 0x1,
        Fill = 0x2,
        EstimateNormals = 0x4,
        Diffuse = 0x8,
        Specular = 0x10,
        UseLut = 0x20,
        ShowLevel = 0x40
    }

    public class MeshSceneElement : ISceneElement
    {
        public MeshRenderFlags Flags { get; set; } = MeshRenderFlags.Fill;
        public float AttributeMin { get; set; } = 0;
        public float AttributeMax { get; set; } = 1;
        public float LevelValue { get; set; } = 0;
        public ReferencedData<Mesh>? Mesh { get; set; }
        public ReferencedData<Vector3[]>? PositionOverride { get; set; }
        public ReferencedData<float[]>? AttributeScalar { get; set; }
        public ReferencedData<Lut>? Lut { get; set; }

        public void ConfigureRenderItem(Project proj, RenderItemBase rib)
        {
            if (rib is not RenderItemMesh ri)
                return;

            ri.RenderWireframe = Flags.HasFlag(MeshRenderFlags.Wireframe);
            ri.RenderFace = Flags.HasFlag(MeshRenderFlags.Fill);
            ri.RenderPoints = false;
            ri.RenderCull = false;
            ri.FillColor = System.Drawing.Color.LightGray;
            ri.PointWireColor = System.Drawing.Color.Black;
            ri.Style = ToStyle(Flags);
            ri.LevelValue = LevelValue;
            ri.RenderBlend = false;
            ri.RenderDepth = true;
            ri.UseDynamicArrays = true;
            ri.ValueMin = AttributeMin; 
            ri.ValueMax = AttributeMax;

            if (Mesh is not null)
            {
                Mesh = ModelUtils.Resolve(proj, Mesh);                    
            }

            if (Mesh is not null && Mesh.IsLoaded)
            {
                ri.Mesh = Mesh.Value;
            }
            else
            {
                ri.Mesh = null;
            }
        }

        private static MeshRenderStyle ToStyle(MeshRenderFlags flags)
        {
            MeshRenderStyle st = 0;

            if(flags.HasFlag(MeshRenderFlags.EstimateNormals))
                st |= MeshRenderStyle.EstimateNormals;

            if (flags.HasFlag(MeshRenderFlags.Diffuse))
                st |= MeshRenderStyle.DiffuseLighting;

            if (flags.Equals(MeshRenderFlags.Specular))
                st |= MeshRenderStyle.PhongBlinn;

            if (flags.HasFlag(MeshRenderFlags.UseLut))
                st |= MeshRenderStyle.ColorLut;

            if (flags.HasFlag(MeshRenderFlags.ShowLevel))
                st |= MeshRenderStyle.ShowValueLevel;

            return st;
        }
    }
}
