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

        public JobItemStatus Run(IJob job, IJobContext ctx)
        {
            if (ctx is not ProjectJobContext pctx)
                return JobItemStatus.Failed;

            return RunInternal(job, pctx) ? JobItemStatus.Completed : JobItemStatus.Failed;
        }

        protected abstract bool RunInternal(IJob job, ProjectJobContext ctx);
    }
}
