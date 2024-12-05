using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    public struct CameraInfo
    {
        public Vector3 Camera, LookAt, Up;
    }

    public interface ICameraControl
    {
        void Grab(Vector2 pt, bool translate);
        void Move(Vector2 pt);
        void Release(Vector2 pt);
        void ResizeViewport(Vector2 size);

        event EventHandler<CameraInfo> UpdateView;

        void Set(Vector3 camera, Vector3 lookat, Vector3 up);
    }
}
