using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Scene
{
    public class ViewerScene
    {
        public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
        public Size Viewport { get; set; }
        public MeshSceneElement? Mesh0 { get; set; }
        public GridSceneElement? Grid { get; set; }

        public override string ToString()
        {
            return string.Format("m0:({0}) g:({1})",
                Mesh0?.Version?.ToString() ?? "",
                Grid?.Version?.ToString() ?? "");
        }
    }
}
