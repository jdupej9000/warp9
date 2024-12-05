using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    public class ArcBallCameraControl : ICameraControl
    {
        // https://github.com/mharrys/arcball/blob/master/src/arcball.hpp
        public event EventHandler<CameraInfo> UpdateView;

        Vector2 viewportSize = Vector2.One;

        public void Grab(Vector2 pt, bool translate)
        {
            Vector2 ptn = 2.0f * (pt / viewportSize - Vector2.One);

            throw new NotImplementedException();
        }

        public void Move(Vector2 pt)
        {
            throw new NotImplementedException();
        }

        public void Release(Vector2 pt)
        {
            viewportSize = pt;
        }

        public void ResizeViewport(Vector2 size)
        {
            throw new NotImplementedException();
        }

        public void Set(Vector3 camera, Vector3 lookat, Vector3 up)
        {
            throw new NotImplementedException();
        }
    }
}
