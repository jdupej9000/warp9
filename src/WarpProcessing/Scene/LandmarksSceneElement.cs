using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using Warp9.Data;
using Warp9.Model;
using Warp9.Viewer;

namespace Warp9.Scene
{
    public class LandmarksSceneElement : ISceneElement
    {
        public LandmarksSceneElement() 
        { }

        ReferencedData<PointCloud>? pcl = null;
        ReferencedData<BufferSegment<Vector3>>? positionOverride = null;
        ReferencedData<BufferSegment<Vector3>>? normalsOverride = null;
        ReferencedData<BufferSegment<uint>>? colorOverride = null;

        [JsonIgnore]
        public RenderItemVersion Version { get; } = new RenderItemVersion();

        [JsonPropertyName("oriented")]
        public bool Oriented { get; set; } = false;

        [JsonPropertyName("enable-color-array")]
        public bool EnableColorArray { get; set; } = false;

        [JsonPropertyName("color-flat")]
        public System.Drawing.Color FlatColor { get; set; } = System.Drawing.Color.Lime;

        [JsonPropertyName("rel-size")]
        public float RelSize { get; set; } = 0.02f;

        [JsonPropertyName("lms")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferencedData<PointCloud>? Landmarks
        {
            get { return pcl; }
            set { pcl = value; Version.Commit(RenderItemDelta.Full); }
        }

        [JsonPropertyName("lms-pos-override")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferencedData<BufferSegment<Vector3>>? PositionOverride
        {
            get { return positionOverride; }
            set { positionOverride = value; Version.Commit(RenderItemDelta.Dynamic); }
        }

        [JsonPropertyName("lms-normal-override")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferencedData<BufferSegment<Vector3>>? NormalOverride
        {
            get { return normalsOverride; }
            set { normalsOverride = value; Version.Commit(RenderItemDelta.Dynamic); }
        }

        [JsonPropertyName("lms-color-override")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferencedData<BufferSegment<uint>>? AttributeScalar
        {
            get { return colorOverride; }
            set { colorOverride = value; Version.Commit(RenderItemDelta.Full); }
        }


        public void ConfigureRenderItem(RenderItemDelta delta, Project proj, RenderItemBase rib)
        {
            if (rib is not RenderItemInstancedMesh ri)
                return;

            if (delta == RenderItemDelta.Full)
            {
            }
            else if (delta.HasFlag(RenderItemDelta.Dynamic))
            {
            }

        }

        public void PersistData(Project project)
        {
            if (pcl is not null && !pcl.HasReference)
            {
                long key = project.AddReferenceDirect(ProjectReferenceFormat.W9Pcl, pcl.Value!);
                pcl.Key = key;
            }

            // Shove all the dynamic updates into a point cloud and store that as 
            // a reference if there are any updates.
            MeshBuilder mb = new MeshBuilder();
            bool dynamicChanged = false;
            if (positionOverride is not null && !positionOverride.HasReference)
            {
                List<Vector3> pos = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
                pos.AddRange(positionOverride.Value!.Data);
                dynamicChanged = true;
            }

            if (normalsOverride is not null && !normalsOverride.HasReference)
            {
                List<Vector3> normal = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Normal, false).Data;
                normal.AddRange(normalsOverride.Value!.Data);
                dynamicChanged = true;
            }

            if (colorOverride is not null && !colorOverride.HasReference)
            {
                List<uint> attr = mb.GetSegmentForEditing<uint>(MeshSegmentSemantic.Color, false).Data;
                attr.AddRange(colorOverride.Value!.Data);
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

                if (colorOverride is not null && !colorOverride.HasReference)
                    colorOverride.Key = dynKey;
            }
        }
    }
}
