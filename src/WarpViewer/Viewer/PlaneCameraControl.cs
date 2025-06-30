using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Warp9.Viewer
{
    public class PlaneCameraControl : ICameraControl
    {
        public event EventHandler<CameraInfo> UpdateView;

        Vector2 viewportSize = Vector2.One;
        float planeDistance = 1.0f;
        Vector3 cur_camera = Vector3.Normalize(new Vector3(1, 1, 1));
        Vector3 cur_up = Vector3.UnitY;
        Vector3 cur_offset = Vector3.Zero;
        float radius = 4f;

        Vector3 initial = Vector3.Zero;

        public void Get(out Matrix4x4 view)
        {
            view = Matrix4x4.Identity;
        }

        public void Grab(Vector2 pt, bool translate)
        {
            initial = MapToPlane(ToScreenRelative(pt));
        }

        public void Move(Vector2 pt)
        {
            Quaternion q = GetRotation(pt);
            Transform(q, out Vector3 cam, out Vector3 up, out Matrix4x4 mat);
            UpdateView?.Invoke(this, new CameraInfo(mat, MakeFullCamera(cam)));
        }

        public void Release(Vector2 pt)
        {
            Quaternion q = GetRotation(pt);
            Transform(q, out Vector3 cam, out Vector3 up, out Matrix4x4 mat);
            UpdateView?.Invoke(this, new CameraInfo(mat, MakeFullCamera(cam)));
            cur_camera = cam;
            cur_up = up;
        }

        public void ResizeViewport(Vector2 size)
        {
            viewportSize = size;
        }

        public void Scroll(float delta)
        {
            radius *= MathF.Pow(1.05f, delta / 100.0f);
            Transform(Quaternion.Identity, out Vector3 cam, out Vector3 up, out Matrix4x4 mat);
            UpdateView?.Invoke(this, new CameraInfo(mat, cam));
        }

        public void Set(Matrix4x4 view)
        {
          
        }

        public void Execute(CameraCommand command)
        {
            throw new NotImplementedException();
        }

        private Vector3 MakeFullCamera(Vector3 cam)
        {
            return radius * cam + cur_offset;
        }

        private void Transform(Quaternion q, out Vector3 cam, out Vector3 up, out Matrix4x4 mat)
        {
            cam = Vector3.Transform(cur_camera, q);
            up = Vector3.Transform(cur_up, q);
            mat = Matrix4x4.CreateLookAtLeftHanded(MakeFullCamera(cam), cur_offset, up);
        }

        private Quaternion GetRotation(Vector2 pt)
        {
            Vector3 en = MapToPlane(ToScreenRelative(pt));
            Vector3 d = en - initial;

            float angle = d.Length() * 0.5f;
            float ca = MathF.Cos(angle);
            float sa = MathF.Sin(angle);

            Vector3 ab_up = Vector3.UnitX;
            Vector3 ab_out = Vector3.UnitY;
            Vector3 p = Vector3.Normalize(ab_out * d.X - ab_up * d.Y) * sa;

            Quaternion q = new Quaternion(p, ca);

            return q;
        }

        private Vector3 MapToPlane(Vector2 pt)
        {
            return new Vector3(pt.X, pt.Y, planeDistance);
        }

        private Vector2 ToScreenRelative(Vector2 pt)
        {
            Vector2 t = 2.0f * pt / viewportSize - Vector2.One;
            return new Vector2(t.X, t.Y);
        }
    }
}
