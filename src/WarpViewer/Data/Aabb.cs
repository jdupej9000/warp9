using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

        public static Aabb Invalid => new Aabb();

        public override string ToString()
        {
            return $"{Min}-{Max}";
        }
    }
}
