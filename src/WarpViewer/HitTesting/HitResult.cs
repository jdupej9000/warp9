using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.HitTesting
{
    public struct HitResult
    {
        public int ObjectId { get; init; }
        public int PrimitiveId { get; init; }
        public Vector3 Position { get; init; }

        public bool IsMiss => ObjectId == -1;

        public static readonly HitResult Miss = new HitResult { ObjectId = -1 };

        public HitResult WithObjectId(int oid)
        {
            return new HitResult { ObjectId = oid, PrimitiveId = PrimitiveId, Position = Position };
        }

        public static int Compare(Vector3 origin, HitResult a, HitResult b)
        {
            if (a.IsMiss)
            {
                if (b.IsMiss)
                    return 0;
                return 1;
            }

            if (b.IsMiss)
                return -1;

            return Vector3.DistanceSquared(origin, a.Position)
                .CompareTo(Vector3.DistanceSquared(origin, b.Position));
        }
    }

    public readonly ref struct RayIntersection
    {
        public RayIntersection(float t0, float t1)
        {
            Entry = t0;
            Exit = t1;
        }

        public float Entry { get; init; }
        public float Exit { get; init; }

        public readonly bool IsHit => Exit >= 0;

        public static RayIntersection Miss => new RayIntersection { Entry = -1, Exit = -1 };
    }
}
