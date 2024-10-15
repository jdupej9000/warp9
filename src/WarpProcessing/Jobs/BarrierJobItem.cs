using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Jobs
{
    public class BarrierJobItem : IJobItem
    {
        public string Title => "(barrier)";

        public JobItemFlags Flags => JobItemFlags.Barrier;

        public JobItemStatus Run(IJob job)
        {
            return JobItemStatus.Completed;
        }
    }
}
