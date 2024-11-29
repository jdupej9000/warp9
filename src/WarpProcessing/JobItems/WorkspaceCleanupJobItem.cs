using Warp9.Jobs;

namespace Warp9.JobItems
{
    public class WorkspaceCleanupJobItem : ProjectJobItem
    {
        public WorkspaceCleanupJobItem(int index, params string[] items) :
            base(index, "Cleanup", JobItemFlags.RunsAlone | JobItemFlags.FailuesAreFatal)
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
