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
        public GridSceneElement? Grid {get; set; }
    }
}
