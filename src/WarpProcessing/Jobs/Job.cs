using System;
using System.CodeDom;
using System.Collections.Generic;

namespace Warp9.Jobs
{
    public enum JobExecutionStatus
    {
        Running,
        RunningTowardsBarrier,
        Done,
        Failed
    }


    public class Job : IJob
    {
        object stateLock = new object();
        readonly List<IJobItem> jobItems = new List<IJobItem>();
        readonly HashSet<int> runningJobs = new HashSet<int>();
        JobExecutionStatus status;

        int nextItemIdx = 0, itemsDone = 0, itemsFailed = 0;

        public IJobContext? Context { get; private set; }
        public int NumItems => jobItems.Count;
        public int NumItemsDone => itemsDone;
        public int NumItemsFailed => itemsFailed;
        public bool IsCompleted => status == JobExecutionStatus.Done || status == JobExecutionStatus.Failed;

        /// <summary>
        /// Tries to pick the next job item to be executed and execute it.
        /// Return true, if a job item has been executed. Returns false if
        /// no job item could be selected due to barriers or no more items
        /// left in the job. Failed items also count as executed, but failed
        /// count gets incremented.
        /// </summary>
        public bool TryExecuteNext()
        {
            IJobContext ctx = Context ?? 
                throw new InvalidOperationException("Cannot execute a job once its context was destroyed.");

            IJobItem? item = null;
            int itemIndex = 0;

            lock (stateLock)
            {
                if (status == JobExecutionStatus.Done ||
                    status == JobExecutionStatus.Failed)
                {
                }
                else if (status == JobExecutionStatus.RunningTowardsBarrier)
                {
                    if(runningJobs.Count == 0 && nextItemIdx < jobItems.Count)
                        item = jobItems[nextItemIdx];
                }
                else if (status == JobExecutionStatus.Running)
                {
                    if (nextItemIdx < jobItems.Count)
                        item = jobItems[nextItemIdx];
                }
                else
                {
                    throw new InvalidOperationException();
                }

                if (item is not null)
                {
                    if (item.Flags.HasFlag(JobItemFlags.WaitsForAllPrevious) &&
                        runningJobs.Count > 0)
                    {
                        item = null;
                    }
                    else if (item.Flags.HasFlag(JobItemFlags.BlocksNext))
                    {
                        status = JobExecutionStatus.RunningTowardsBarrier;
                    }
                    else
                    {
                        status = JobExecutionStatus.Running;
                    }
                }

                if (item is not null)
                {
                    itemIndex = nextItemIdx++;
                    runningJobs.Add(itemIndex);
                }
            }

            if (item is not null)
            {
                JobItemStatus itemStatus;
                try
                {
                    itemStatus = item.Run(this, ctx);
                }
                catch (Exception)
                {
                    itemStatus = JobItemStatus.Failed;
                }
               
                lock (stateLock)
                {
                    if (itemStatus == JobItemStatus.Failed)
                        itemsFailed++;

                    itemsDone++;
                    runningJobs.Remove(itemIndex);
                }

                return true;
            }

            return false;
        }

        public IJobContext DetachContext()
        {
            IJobContext ctx = Context ?? throw new InvalidOperationException("Context already detached.");
            Context = null;
            return ctx;
        }

        public static Job Create(IEnumerable<IJobItem> items, IJobContext ctx)
        {
            Job ret = new Job();
            ret.jobItems.AddRange(items);
            ret.Context = ctx;
            return ret;
        }
    }
}
