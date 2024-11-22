using System;
using System.Collections.Generic;
using System.Threading;

namespace Warp9.Jobs
{
    public interface IJobWithContext
    {
        public IJob Job { get; }
        public IJobContext? Context { get; }
        public bool IsDone { get; }

        public void RemoveContext();
    }

    internal class BackgroundWorkerContext(int threadIndex, JobEngine engine)
    {
        internal int ThreadIndex { get; init; } = threadIndex;
        internal JobEngine Engine { get; init; } = engine;
        internal AutoResetEvent Notification { get; init; } = new AutoResetEvent(false);
        internal bool MustTerminate { get; set; } = false;
    }

    // Jobs are batches of JobItems that are executed over a IJobContext. JobItems read and write
    // data (entries and references) to/from a Project (if it is a ProjectJobContext) and share
    // intermediate data in JobWorkspace as (key,index)->object. JobEngine spawns worker threads 
    // and these juggle the Jobs which have not been completed. A worker tries to take one IJobWithContext
    // from the job's queue and execute it. Jobs can have barriers before or after them for sychronizaiton.
    // If a worker cannot execute a JobItem from a Job (e.g. due to a barrier waiting), it tries to 
    // take an execute from the next Job. Jobs are round-robined by the workers for fairness. When 
    // a JobItem finishes, its IJobWithContext is stripped of the context, to remove any references
    // to large data. To remove superfluous data in JobWorkspace mid-Job, add JobItems that do the
    // cleanup and protect them with barriers. When a Job completes, its JobWorkspace is destroyed.
    public class JobEngine : IDisposable
    {
        public JobEngine()
        {
            workerCount = Environment.ProcessorCount;

            workers = new Thread[workerCount];
            contexts = new BackgroundWorkerContext[workerCount];
            for (int i = 0; i < workerCount; i++)
            {
                contexts[i] = new BackgroundWorkerContext(i, this);
                workers[i] = new Thread(BackgroundWorkerProc);
                workers[i].Start(contexts[i]);
            }
        }

        private object contextLock = new object();
        private readonly int workerCount;
        private readonly Thread[] workers;
        private BackgroundWorkerContext[] contexts;
        private List<IJobWithContext> jobs = new List<IJobWithContext>();
        private List<IJobWithContext> finishedJobs = new List<IJobWithContext>();

        public List<IJobWithContext> Jobs => jobs;
        public List<IJobWithContext> FinishedJobs => finishedJobs;

        public void Run(IJobWithContext job)
        {
            lock (contextLock)
                jobs.Add(job);

            NotifyWorkers();
        }

        public static void RunImmediately(IJobWithContext job)
        {
            IJobContext ctx = job.Context ?? throw new ArgumentNullException(nameof(job));
            while (!job.IsDone && job.Job.TryExecuteNext(ctx)) ;

        }

        public void Dispose()
        {
            TerminateAll();
        }

        private void NotifyWorkers()
        {
            for (int i = 0; i < workerCount; i++)
                contexts[i].Notification.Set();
        }

        private void JobStatusChanged(IJobWithContext job)
        {
            lock (contextLock)
            {
                if (job.IsDone)
                {
                    job.RemoveContext();
                    finishedJobs.Add(job);
                    jobs.Remove(job);
                }
            }

            NotifyWorkers();
        }

        private IJobWithContext? GetNextJob(ref int idx)
        {
            lock (contextLock)
            {
                if (idx >= jobs.Count)
                    idx = 0;

                if (idx < jobs.Count)
                {
                    int i = idx++;
                    return jobs[i];
                }
            }

            return null;
        }


        private void TerminateAll()
        {
            for (int i = 0; i < workerCount; i++)
                contexts[i].MustTerminate = true;

            NotifyWorkers();
        }

        private static void BackgroundWorkerProc(object? p)
        {
            if (p is not BackgroundWorkerContext ctx)
                throw new ArgumentException(nameof(p));

            int jobIndex = 0;

            while (true)
            {
                IJobWithContext? firstBlockedJob = null;
                IJobWithContext? nextJob = ctx.Engine.GetNextJob(ref jobIndex);
                while (nextJob is not null)
                {
                    if (nextJob.Context is null)
                        throw new InvalidOperationException();

                    bool itemTaken = nextJob.Job.TryExecuteNext(nextJob.Context);

                    // If we go through the entire job list and no job items get completed,
                    // break out of the loop as we are obviously waiting for dependencies on
                    // all of them and wait for a notification.
                    if (!itemTaken && firstBlockedJob is null)
                    {
                        firstBlockedJob = nextJob;
                    }
                    else if (!itemTaken && firstBlockedJob == nextJob)
                    {
                        firstBlockedJob = null;
                        break;
                    }
                    else if (itemTaken)
                    {
                        firstBlockedJob = null;
                        ctx.Engine.JobStatusChanged(nextJob);
                    }

                    if (ctx.MustTerminate)
                        return;

                    nextJob = ctx.Engine.GetNextJob(ref jobIndex);
                }

                if (ctx.MustTerminate)
                    return;

                ctx.Notification.WaitOne();

                if (ctx.MustTerminate)
                    return;
            }
        }
    }
}