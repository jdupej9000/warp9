using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.HitTesting
{
    public interface IHitTestItem
    {

        RayIntersection TestCoarse(Vector3 origin, Vector3 direction);
        HitResult Test(Vector3 origin, Vector3 direction, float maxDistance);
    }
}
