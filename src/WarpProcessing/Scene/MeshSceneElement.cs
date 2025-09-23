using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
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

        public MeshSceneElement Duplicate()
        {
            MeshSceneElement ret = new MeshSceneElement();
            ret.Flags = Flags;
            ret.AttributeMin = AttributeMin;
            ret.AttributeMax = AttributeMax;
            ret.LevelValue = LevelValue;
            ret.FlatColor = FlatColor;
            ret.mesh = mesh;
            ret.positionOverride = positionOverride;
            ret.normalsOverride = normalsOverride;
            ret.attributeScalar = attributeScalar;
            ret.lutSpec = lutSpec;

            return ret;
        }

        public void PersistData(Project project)
        {
            // Add the mesh as a reference unless it has been loaded from a reference.
            if (mesh is not null && !mesh.HasReference)
            {
                long key = project.AddReferenceDirect(ProjectReferenceFormat.W9Mesh, mesh.Value!);
                mesh.Key = key;
            }

            // Shove all the dynamic updates into a point cloud and store that as 
            // a reference if there are any updates.
            MeshBuilder mb = new MeshBuilder();
            bool dynamicChanged = false;
            if (positionOverride is not null && !positionOverride.HasReference)
            {
                List<Vector3> pos = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);
                pos.AddRange(positionOverride.Value!);
                dynamicChanged = true;
            }

            if (normalsOverride is not null && !normalsOverride.HasReference)
            {
                List<Vector3> normal = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Normal);
                normal.AddRange(normalsOverride.Value!);
                dynamicChanged = true;
            }

            if (attributeScalar is not null && !attributeScalar.HasReference)
            {
                List<float> attr = mb.GetSegmentForEditing<float>(MeshSegmentSemantic.AttribScalar);
                attr.AddRange(attributeScalar.Value!);
                dynamicChanged = true;
            }

            if (dynamicChanged)
            {
                PointCloud pclDyn = mb.ToPointCloud();
                long dynKey = project.AddReferenceDirect(ProjectReferenceFormat.W9Pcl, pclDyn);

                if (positionOverride is not null && !positionOverride.HasReference)
                    positionOverride.Key = dynKey;

                if (normalsOverride is not null && !normalsOverride.HasReference)
                    normalsOverride.Key = dynKey;

                if (attributeScalar is not null && !attributeScalar.HasReference)
                    attributeScalar.Key = dynKey;
            }
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
                ri.UpdateData(positionOverride.Value, MeshSegmentSemantic.Position);
                changed = true;
            }

            if (normalsOverride is not null && normalsOverride.IsLoaded && normalsOverride.Value is not null)
            {
                ri.UpdateData(normalsOverride.Value, MeshSegmentSemantic.Normal);
                changed = true;
            }

            if (changed)
                ri.Version.Commit(RenderItemDelta.Dynamic);
        }

        private void ResolveReferences(Project proj)
        {
            if (mesh is not null)
                mesh = ModelUtils.Resolve(proj, mesh);

            if (positionOverride is not null)
                positionOverride = ModelUtils.ResolveAsMeshView(proj, MeshViewKind.Pos3f, positionOverride);

            if (normalsOverride is not null)
                normalsOverride = ModelUtils.ResolveAsMeshView(proj, MeshViewKind.Normal3f, normalsOverride);

            if (attributeScalar is not null)
                attributeScalar = ModelUtils.ResolveAsMeshView(proj, MeshViewKind.Attrib1f, attributeScalar);
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
