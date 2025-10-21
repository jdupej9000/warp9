using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Jobs;

namespace Warp9.JobItems
{
    public class BarrierJobItem(int index) : 
        ProjectJobItem(index, "Wait for previous items", JobItemFlags.WaitsForAllPrevious)
    {
        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            return true;
        }
    }
}
