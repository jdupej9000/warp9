using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Scene
{
    public class ViewerScene
    {
        [JsonPropertyName("view")]
        public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;

        [JsonPropertyName("viewport")]
        public Size Viewport { get; set; }

        [JsonPropertyName("mesh0")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MeshSceneElement? Mesh0 { get; set; }

        [JsonPropertyName("grid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GridSceneElement? Grid { get; set; }

        public IEnumerable<ISceneElement> EnumSceneElements()
        {
            if (Mesh0 is not null) yield return Mesh0;
            if (Grid is not null) yield return Grid;
        }

        public ViewerScene Duplicate()
        {
            ViewerScene ret = new ViewerScene();
            ret.ViewMatrix = ViewMatrix;
            ret.Viewport = Viewport;
            ret.Mesh0 = Mesh0?.Duplicate();
            ret.Grid = Grid?.Duplicate();
            return ret;
        }

        public override string ToString()
        {
            return string.Format("m0:({0}) g:({1})",
                Mesh0?.Version?.ToString() ?? "",
                Grid?.Version?.ToString() ?? "");
        }
    }
}
