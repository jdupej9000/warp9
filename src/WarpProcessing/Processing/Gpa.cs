using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Jobs;

namespace Warp9.Processing
{
    public class GpaConfiguration
    {
    }

    public class Gpa
    {
        public PointCloud Mean { get; private set; } = PointCloud.Empty;

        public static Gpa Fit(PointCloud[] data, GpaConfiguration? cfg = null)
        {
            throw new NotImplementedException();
        }
    }
}
