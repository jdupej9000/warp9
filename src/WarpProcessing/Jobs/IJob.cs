namespace Warp9.Jobs
{
    public interface IJob
    {
        public int NumItems { get; }
        public int NumItemsDone { get; }
        public int NumItemsFailed { get; }

        public bool TryExecuteNext(IJobContext ctx);
    }
}
