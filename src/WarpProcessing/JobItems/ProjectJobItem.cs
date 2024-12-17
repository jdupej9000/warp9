using System;
using System.Diagnostics;
using System.Text;
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

            JobItemStatus ret = JobItemStatus.Failed;
            string? msg = null;

            try
            {
                ret = RunInternal(job, pctx) ? JobItemStatus.Completed : JobItemStatus.Failed;
            }
            catch (Exception e)
            {
                msg = e.Message + Environment.NewLine + e.StackTrace;
                ret = JobItemStatus.Failed;
            }

            if (ret != JobItemStatus.Completed)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("The task #{0} ({1}) has failed. ", ItemIndex, Title);

                if (Flags.HasFlag(JobItemFlags.FailuesAreFatal))
                    sb.Append("The job will be terminated. ");
                else
                    sb.Append("This is a nonfatal failure. ");

                if (msg is not null)
                    sb.Append("Additional information follows." + Environment.NewLine + msg);

                pctx.WriteLog(ItemIndex, MessageKind.Error, sb.ToString());
            }

            return ret;
        }

        protected abstract bool RunInternal(IJob job, ProjectJobContext ctx);
    }
}
