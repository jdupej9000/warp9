namespace Warp9.Jobs
{
    public class WorkspaceCleanupJobItem : ProjectJobItem   
    {
        public WorkspaceCleanupJobItem(params string[] items) :
            base("Cleanup", JobItemFlags.RunsAlone | JobItemFlags.FailuesAreFatal)
        {
            CleanupItems = items;
        }

        public string[] CleanupItems { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            foreach (string item in CleanupItems)
                ctx.Workspace.Remove(item);

            return true;
        }
    }
}
