namespace Warp9.Jobs
{
    public interface IJob
    {
        public int NumItems { get; }
        public int NumItemsDone { get; }
        public int NumItemsFailed { get; }
        public bool IsCompleted { get; }

        public IJobContext? Context { get; }

        public bool TryExecuteNext();

        public IJobContext DetachContext();
    }
}
