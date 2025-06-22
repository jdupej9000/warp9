using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Stage
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
        UseLut = 0x20
    }

    public class ReferencedData<T>
    {
        public long Key { get; set; } = -1;
        public T? Value { get; set; }
    }

    public class MeshStageElement
    {
        public bool Visible { get; set; }
        public MeshRenderFlags Flags { get; set; } = MeshRenderFlags.Fill;
        public float AttributeMin { get; set; } = 0;
        public float AttributeMax { get; set; } = 1;
        public ReferencedData<Mesh>? Mesh { get; set; }
        public ReferencedData<Vector3[]>? PositionOverride { get; set; }
        public ReferencedData<float>? AttributeScalar { get; set; }
        public ReferencedData<Lut>? Lut { get; set; }
    }

    public class GridStageElement
    {
        public bool Visible { get; set; }
    }

    public class ViewerStage
    {
        public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
        public Size Viewport { get; set; }

        public MeshStageElement? Mesh0 { get; set; }

        public GridStageElement? Grid {get; set; }
    }
}
