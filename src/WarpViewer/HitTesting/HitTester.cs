using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Viewer;

namespace Warp9.HitTesting
{
    public class HitTester
    {
        Dictionary<int, IHitTestItem> items = new Dictionary<int, IHitTestItem>();

        public HitResult TestAll(Vector3 origin, Vector3 direction)
        {
            float normalize = 1.0f / direction.Length();
            HitResult hbest = HitResult.Miss;
            float tbest = float.MaxValue;
            int ibest = int.MinValue;

            foreach (var kvp in items)
            {
                RayIntersection coarse = kvp.Value.TestCoarse(origin, direction);

                if (!coarse.IsHit || coarse.Entry >= tbest)
                    continue;

                HitResult exact = kvp.Value.Test(origin, direction, tbest);
                if (!exact.IsMiss)
                {
                    float t = (exact.Position - origin).Length() * normalize;
                    if (t < tbest)
                    {
                        tbest = t;
                        ibest = kvp.Key;
                        hbest = exact;
                    }
                }
            }

            return hbest;
        }

        public HitResult TestAll(Vector2 screenOrigin, Vector2 screenSize, Matrix4x4 view, Matrix4x4 proj)
        {
            if (!Matrix4x4.Invert(view * proj, out Matrix4x4 viewProjInv))
                return HitResult.Miss;

            // https://stackoverflow.com/questions/46182845/field-of-view-aspect-ratio-view-matrix-from-projection-matrix-hmd-ost-calib
            float fov = 2 * MathF.Atan(1.0f / proj.M22);
            float aspect = proj.M22 / proj.M11;
            float near = 0.01f;
            float far = 100.0f;
            float dy = MathF.Tan(fov * 0.5f) * (1.0f - screenOrigin.Y / screenSize.Y);
            float dx = MathF.Tan(fov * 0.5f) * (screenOrigin.X / screenSize.X - 1.0f) / aspect;

            Vector3 pp1 = Vector3.Transform(new Vector3(dx * near, dy * near, near), viewProjInv);
            Vector3 pp2 = Vector3.Transform(new Vector3(dx * far, dy * far, far), viewProjInv);

            Matrix4x4.Invert(view, out Matrix4x4 viewi);
            Vector3 camera = new Vector3(viewi.M41, viewi.M42, viewi.M43);

            return TestAll(camera, Vector3.Normalize(pp2 - pp1));
        }
    }
}
