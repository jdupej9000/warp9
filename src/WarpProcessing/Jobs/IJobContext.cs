namespace Warp9.Jobs
{
    public interface IJobContext
    {
        public JobWorkspace Workspace { get; }
    }
}
