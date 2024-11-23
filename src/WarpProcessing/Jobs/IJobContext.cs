namespace Warp9.Jobs
{
    public enum MessageKind
    {
        Information,
        Warning,
        Error
    }

    public interface IJobContext
    {
        JobWorkspace Workspace { get; }

        void WriteLog(int jobItemIndex, MessageKind kind, string message);
    }
}
