using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;

namespace Warp9.Jobs
{
    public class ProjectJob : IJob
    {
        public JobWorkspace Workspace { get; init; } = new JobWorkspace();
        public Project Project { get; init; }

        public JobItemStatus ExecuteItem(IJobItem item)
        {
            if (item is ProjectJobItem ji)
                return ji.Run(this);
            else
                return JobItemStatus.Failed;
        }

    }
}
