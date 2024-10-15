using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;

namespace Warp9.Jobs
{
    public abstract class ProjectJobItem : IJobItem
    {
        public ProjectJobItem(string title, JobItemFlags flags = JobItemFlags.None)
        {
            Title = title;
            Flags = flags;
        }

        public string Title { get; init; }

        public JobItemFlags Flags { get; init; }

        public JobItemStatus Run(IJob job)
        {
            if (job is not ProjectJob pjob)
                return JobItemStatus.Failed;

            return RunInternal(pjob) ? JobItemStatus.Completed : JobItemStatus.Failed;
        }

        protected abstract bool RunInternal(ProjectJob job);
    }
}
