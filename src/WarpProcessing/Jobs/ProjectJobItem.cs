namespace Warp9.Jobs
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
                return JobItemStatus.Failed;

            return RunInternal(job, pctx) ? JobItemStatus.Completed : JobItemStatus.Failed;
        }

        protected abstract bool RunInternal(IJob job, ProjectJobContext ctx);
    }
}
