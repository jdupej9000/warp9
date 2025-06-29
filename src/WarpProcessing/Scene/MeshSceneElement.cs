﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
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
        public MeshSceneElement() 
        {
        }

        ReferencedData<Mesh>? mesh = null;
        ReferencedData<Vector3[]>? positionOverride = null;
        ReferencedData<Vector3[]>? normalsOverride = null;
        ReferencedData<float[]>? attributeScalar = null;
        LutSpec? lutSpec = null;
        Lut? lut = null;
        bool lutChanged = false;

        const int LutWidth = 256;

        [JsonIgnore]
        public RenderItemVersion Version { get; } = new RenderItemVersion();

        [JsonPropertyName("flags")]
        public MeshRenderFlags Flags { get; set; } = MeshRenderFlags.Fill | MeshRenderFlags.Diffuse;

        [JsonPropertyName("attr-min")]
        public float AttributeMin { get; set; } = 0;

        [JsonPropertyName("attr-max")]
        public float AttributeMax { get; set; } = 1;

        [JsonPropertyName("level")]
        public float LevelValue { get; set; } = 0;

        [JsonPropertyName("color-flat")]
        public System.Drawing.Color FlatColor { get; set; } = System.Drawing.Color.LightGray;

        [JsonPropertyName("mesh")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferencedData<Mesh>? Mesh
        {
            get { return mesh; }
            set { mesh = value; Version.Commit(RenderItemDelta.Full); }
        }

        [JsonPropertyName("mesh-pos-override")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferencedData<Vector3[]>? PositionOverride
        {
            get { return positionOverride; }
            set { positionOverride = value; Version.Commit(RenderItemDelta.Dynamic); }
        }

        [JsonPropertyName("mesh-normal-override")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferencedData<Vector3[]>? NormalOverride
        {
            get { return normalsOverride; }
            set { normalsOverride = value; Version.Commit(RenderItemDelta.Dynamic); }
        }

        [JsonPropertyName("mesh-attrsc-override")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferencedData<float[]>? AttributeScalar
        {
            get { return attributeScalar; }
            set { attributeScalar = value; Version.Commit(RenderItemDelta.Full); }
        }

        [JsonPropertyName("lut-spec")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LutSpec? LutSpec
        {
            get { return lutSpec; }
            set { lutSpec = value; Version.Commit(RenderItemDelta.Full); }
        }

        public void ConfigureRenderItem(RenderItemDelta delta, Project proj, RenderItemBase rib)
        {
            if (rib is not RenderItemMesh ri)
                return;

            if (delta == RenderItemDelta.Full)
                ConfigureFull(proj, ri);

            if (delta.HasFlag(RenderItemDelta.Dynamic))
                ConfigureDynamic(proj, ri);

            ri.RenderWireframe = Flags.HasFlag(MeshRenderFlags.Wireframe);
            ri.RenderFace = Flags.HasFlag(MeshRenderFlags.Fill);
            ri.Style = ToStyle(Flags);
            ri.LevelValue = LevelValue;
            ri.ValueMin = AttributeMin;
            ri.ValueMax = AttributeMax;
            ri.RenderPoints = false;
            ri.RenderCull = false;
            ri.FillColor = FlatColor;
            ri.PointWireColor = System.Drawing.Color.Black;
            ri.RenderBlend = false;
            ri.RenderDepth = true;
        }

        private void ConfigureFull(Project proj, RenderItemMesh ri)
        {
            ResolveReferences(proj);
            ri.UseDynamicArrays = true;

            ri.Mesh = (mesh is not null && mesh.IsLoaded) ? mesh.Value : null;
                        
            if (lutSpec is null)
            {
                ri.Lut = null;
            }
            else
            {
                lut = Lut.Create(LutWidth, lutSpec);
                ri.Lut = lut;
            }

            if (attributeScalar is not null && attributeScalar.IsLoaded && attributeScalar.Value is not null)
                ri.SetValueField(attributeScalar.Value);

            ri.Version.Commit(RenderItemDelta.Full);
        }

        private void ConfigureDynamic(Project proj, RenderItemMesh ri)
        {
            bool changed = false;

            if (positionOverride is not null && positionOverride.IsLoaded && positionOverride.Value is not null)
            {
                ri.UpdateData(positionOverride.Value, MeshSegmentType.Position);
                changed = true;
            }

            if (normalsOverride is not null && normalsOverride.IsLoaded && normalsOverride.Value is not null)
            {
                ri.UpdateData(normalsOverride.Value, MeshSegmentType.Normal);
                changed = true;
            }

            if (changed)
                ri.Version.Commit(RenderItemDelta.Dynamic);
        }

        private void ResolveReferences(Project proj)
        {
            if (mesh is not null)
                mesh = ModelUtils.Resolve(proj, mesh);

            // TODO: dynamic overrides
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
