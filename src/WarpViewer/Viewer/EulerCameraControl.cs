using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    public class EulerCameraControl : ICameraControl
    {
        public event EventHandler<CameraInfo> UpdateView;

        Vector2 viewportSize = Vector2.One;
        Vector2 rotationSpeed = new Vector2(2 * MathF.PI, 2 * MathF.PI);

        Matrix4x4 prevRot = Matrix4x4.Identity;
        Vector3 prevTrans = Vector3.Zero;
        Vector2 pt0 = Vector2.Zero;
        float radius = 4f;
        bool isTranslating = false;

        public void Grab(Vector2 pt, bool translate)
        {
            pt0 = pt;
            isTranslating = translate;
        }

        public void Move(Vector2 pt)
        {
            if (isTranslating)
            {
                // TODO;
            }
            else
            {
                Vector2 delta = (pt - pt0) / viewportSize * rotationSpeed;
                Update(delta, Vector3.Zero);
            }
        }

        public void Release(Vector2 pt)
        {
            if (isTranslating)
            {
                // TODO;
            }
            else
            {
                Vector2 delta = (pt - pt0) / viewportSize * rotationSpeed;
                Update(delta, Vector3.Zero, true);
            }
        }

        public void Scroll(float delta)
        {
            radius *= MathF.Pow(1.05f, delta / 100.0f);
            Update(Vector2.Zero, Vector3.Zero);
        }


        public void ResizeViewport(Vector2 size)
        {
            viewportSize = size;
        }

        public void Set(Matrix4x4 view)
        {
            Matrix4x4.Decompose(view, out Vector3 scale, out Quaternion rot, out Vector3 trans);
            prevRot = Matrix4x4.CreateFromQuaternion(rot);
            prevTrans = trans;
        }

        public void Get(out Matrix4x4 view)
        {
            view = Matrix4x4.Identity;
        }

        public void Execute(CameraCommand command)
        {
            switch (command)
            {
                case CameraCommand.SetFront:
                    prevRot = Matrix4x4.CreateFromYawPitchRoll(0, 0, 0);
                    break;
                case CameraCommand.SetBack:
                    prevRot = Matrix4x4.CreateFromYawPitchRoll(MathF.PI, 0, 0);
                    break;
                case CameraCommand.SetRight:
                    prevRot = Matrix4x4.CreateFromYawPitchRoll(MathF.PI / 2, 0, 0);
                    break;
                case CameraCommand.SetLeft:
                    prevRot = Matrix4x4.CreateFromYawPitchRoll(-MathF.PI / 2, 0, 0);
                    break;
                case CameraCommand.SetTop:
                    prevRot = Matrix4x4.CreateFromYawPitchRoll(0, MathF.PI / 2, 0);
                    break;
                case CameraCommand.SetBottom:
                    prevRot = Matrix4x4.CreateFromYawPitchRoll(0, -MathF.PI / 2, 0);
                    break;
            }

            Update(Vector2.Zero, Vector3.Zero, true);
        }

        private void Update(Vector2 rotDelta, Vector3 transDelta, bool release = false)
        {
            Matrix4x4 newRot = Matrix4x4.Multiply(Matrix4x4.CreateFromYawPitchRoll(rotDelta.X, rotDelta.Y, 0), prevRot);

            Matrix4x4 newTrans = Matrix4x4.CreateTranslation(prevTrans + transDelta + new Vector3(0, 0, radius));
            Matrix4x4 newXform = Matrix4x4.Multiply(newRot, newTrans);

            Matrix4x4.Invert(newXform, out Matrix4x4 viewi);
            Vector3 camera = new Vector3(viewi.M41, viewi.M42, viewi.M43);

            UpdateView?.Invoke(this, new CameraInfo(newXform, camera));

            if (release)
            {
                prevRot = newRot;
                prevTrans += transDelta;
            }
        }
    }
}
