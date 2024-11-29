using System;
using Warp9.Jobs;

namespace Warp9.JobItems
{
    public abstract class ProjectJobItem : IJobItem
    {
        public ProjectJobItem(int index, string title, JobItemFlags flags = JobItemFlags.None)
        {
            ItemIndex = index;
            Title = title;
            Flags = flags;
        }

        public int ItemIndex { get; init; }
        public string Title { get; init; }

        public JobItemFlags Flags { get; init; }

        public JobItemStatus Run(IJob job, IJobContext ctx)
        {
            if (ctx is not ProjectJobContext pctx)
                throw new ArgumentException("ctx is not a ProjectJobContext.");

            try
            {
                return RunInternal(job, pctx) ? JobItemStatus.Completed : JobItemStatus.Failed;
            }
            catch (Exception e)
            {
                pctx.WriteLog(ItemIndex, MessageKind.Error,
                    "Job item failed with an exception. " + e.Message + Environment.NewLine + e.StackTrace);
                return JobItemStatus.Failed;
            }
        }

        protected abstract bool RunInternal(IJob job, ProjectJobContext ctx);
    }
}
