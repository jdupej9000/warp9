using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    public class ArcBallCameraControl : ICameraControl
    {
        // https://stackoverflow.com/questions/23747013/arcball-controls-with-qt-and-opengl
        public event EventHandler<CameraInfo> UpdateView;

        Vector2 viewportSize = Vector2.One;
        Vector3 st;
        Vector3 camera = Vector3.Normalize(new Vector3(1, 1, 1));
        Vector3 up = Vector3.UnitY;
        Vector3 offset = Vector3.Zero;
        float radius = 4f;

        public void Grab(Vector2 pt, bool translate)
        {
            st = ToSphere(ToScreenRelative(pt));
        }

        public void Move(Vector2 pt)
        {
            Quaternion rot = GetRotation(pt);
            Vector3 camera2 = Vector3.Transform(camera, rot);
            Vector3 up2 = Vector3.Transform(up, rot);
            Vector3 camera2full = radius * camera2 + offset;

            Matrix4x4 view = Matrix4x4.CreateLookAtLeftHanded(camera2full, offset, up2);
            UpdateView?.Invoke(this, new CameraInfo(view, camera2full));
        }


        public void Release(Vector2 pt)
        {
            Quaternion rot = GetRotation(pt);
            Vector3 camera2 = Vector3.Transform(camera, rot);
            Vector3 up2 = Vector3.Transform(up, rot);
            Vector3 camera2full = radius * camera2 + offset;

            Matrix4x4 view = Matrix4x4.CreateLookAtLeftHanded(camera2full, offset, up2);
            UpdateView?.Invoke(this, new CameraInfo(view, camera2full));

            camera = camera2;
            up = up2;
        }

        public void Scroll(float delta)
        {
            radius *= MathF.Pow(1.05f, delta / 100.0f);
            Vector3 camera2full = radius * camera + offset;
            Matrix4x4 view = Matrix4x4.CreateLookAtLeftHanded(camera2full, offset, up);
            UpdateView?.Invoke(this, new CameraInfo(view, camera2full));
        }


        public void ResizeViewport(Vector2 size)
        {
            viewportSize = size;
        }

        public void Set(Matrix4x4 view)
        {
            
        }
       
        public void Get(out Matrix4x4 view)
        {
            view = Matrix4x4.Identity;
        }

        public void Execute(CameraCommand command)
        {
            throw new NotImplementedException();
        }

        private Quaternion GetRotation(Vector2 pt)
        {
            Vector3 en = ToSphere(ToScreenRelative(pt));
            
            float alpha = MathF.Acos(MathF.Min(1, Vector3.Dot(en, st)));
            Vector3 axis = Vector3.Normalize(Vector3.Cross(st, en));

            return Quaternion.CreateFromAxisAngle(axis, -alpha);
        }

        private Vector2 ToScreenRelative(Vector2 pt)
        {
            Vector2 t = 2.0f * pt / viewportSize - Vector2.One;
            return new Vector2(t.X, -t.Y);
            //return t;
        }

        private Vector3 ToSphere(Vector2 pt)
        {
            float len = pt.LengthSquared();
            Vector2 ptyx = new Vector2(pt.X, -pt.Y);

            if (len > 1)
            {
                return Vector3.Normalize(new Vector3(ptyx, 0));
            }
            else
            {
                return Vector3.Normalize(new Vector3(ptyx, MathF.Sqrt(1.0f - len)));
            }
        }
    }
}
