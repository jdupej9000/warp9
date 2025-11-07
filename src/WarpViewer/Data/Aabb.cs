using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using Warp9.HitTesting;

namespace Warp9.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Aabb
    {
        public Aabb()
        {
            Min = Vector3.Create(float.MaxValue);
            Max = Vector3.Create(float.MinValue);
        }

        public Aabb(Vector3 p0, Vector3 p1)
        {
            Min = p0;
            Max = p1;
        }

        public Vector3 Min;
        public Vector3 Max;
        public bool IsInvalid => Min.X > Max.X || Min.Y > Max.Y || Min.Z > Max.Z;
        public float MaxSide => MathF.Max(MathF.Max(Max.X - Min.X, Max.Y - Min.Y), Max.Z - Min.Z);

        public static Aabb Invalid => new Aabb();

        public readonly bool Contains(Vector3 pt)
        {
            return pt.X >= Min.X && pt.X <= Max.X &&
                pt.Y >= Min.Y && pt.Y <= Max.Y &&
                pt.Z >= Min.Z && pt.Z <= Max.Z;
        }

        public override string ToString()
        {
            return $"{Min}-{Max}";
        }

        public readonly RayIntersection IntersectRay(Vector3 o, Vector3 d)
        {
            Vector128<float> cutoff = Vector128.Create(1e-8f);

            Vector128<float> dd = d.AsVector128();
            Vector128<float> cmp = Vector128.GreaterThan(Vector128.Abs(dd), cutoff);
            dd = Vector128.ConditionalSelect(cmp, dd, cutoff);

            uint mask = Vector128.ExtractMostSignificantBits(cmp);

            Vector128<float> k0 = Vector128.Divide((Min - o).AsVector128(), dd);
            Vector128<float> k1 = Vector128.Divide((Max - o).AsVector128(), dd);

            float tmin = 0, tmax = 1e30f;
            for (int i = 0; i < 3; i++)
            {
                if ((mask >> i) != 0)
                {
                    float k0i = k0[i];
                    float k1i = k1[i];

                    tmin = MathF.Max(tmin, MathF.Min(k0i, k1i));
                    tmax = MathF.Min(tmin, MathF.Max(k0i, k1i));
                }
            }

            if (tmax > 0 && tmin < tmax)
                return new RayIntersection(tmin, tmax);

            return RayIntersection.Miss;
        }
    }
}
