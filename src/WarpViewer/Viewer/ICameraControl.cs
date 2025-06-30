using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    public enum CameraCommand
    {
        SetFront,
        SetBack,
        SetRight,
        SetLeft,
        SetTop,
        SetBottom           
    }

    public struct CameraInfo
    {
        public CameraInfo(Matrix4x4 view, Vector3 cam)
        {
            ViewMat = view;
            CameraPos = cam;
        }

        public Matrix4x4 ViewMat;
        public Vector3 CameraPos;
    }

    public interface ICameraControl
    {
        void Grab(Vector2 pt, bool translate);
        void Move(Vector2 pt);
        void Release(Vector2 pt);
        void Scroll(float delta);
        void ResizeViewport(Vector2 size);

        event EventHandler<CameraInfo> UpdateView;

        void Set(Matrix4x4 view);
        void Get(out Matrix4x4 view);

        void Execute(CameraCommand command);
    }
}
