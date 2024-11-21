using System.Collections.ObjectModel;
using System.ComponentModel;
using Warp9.Jobs;

namespace Warp9.Utils
{

    public class BackgroundJob : INotifyPropertyChanged, IJobWithContext
    {
        public BackgroundJob(IJob job, IJobContext ctx)
        {
            Job = job;
            Context = ctx;
        }

        public IJob Job { get; init; }
        public IJobContext? Context { get; set; }
        public string Status => MakeStatus();
        public bool IsDone => Job.NumItemsDone == Job.NumItems;

        public event PropertyChangedEventHandler? PropertyChanged;  // TODO: call this sometimes

        public void RemoveContext()
        {
            Context = null;
        }

        private string MakeStatus()
        {
            if (false)
                return "Canceled";
            else if (Job.NumItemsFailed > 0)
                return string.Format("{0}/{1} done, {2} errors", Job.NumItemsDone, Job.NumItems, Job.NumItemsFailed);
            else if (IsDone)
                return "All done";
            else
                return string.Format("{0}/{1} done", Job.NumItemsDone, Job.NumItems);
        }

    }
}
