using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Transactions;

namespace Warp9.Utils
{
    public enum JobItemStatus
    {
        Waiting,
        Running,
        Completed,
        Failed
    }

    public interface IJobLog
    {
    }

    public interface IJobContext
    {
        public IJobLog Log { get; }
        public bool IsCancelled { get; }

        public void JobItemDone(long key, JobItemStatus status);
    }

    public class BackgroundJobItem
    {
        public long Key { get; init; }
        public string Title { get; init; }
        public bool FailureCancelsJob { get; init; }

        public bool Execute(IJobContext ctx)
        {
            return false;
        }
    }

    public class BackgroundJob : IJobContext
    {
        public IJobLog Log => throw new NotImplementedException();
        public bool IsCancelled { get; set; } = false;
       

        public bool FindNextJobItem([MaybeNullWhen(false)] out BackgroundJobItem? item)
        {
            item = null;
            return false;
        }

        public void JobItemDone(long key, JobItemStatus status)
        {
            throw new NotImplementedException();
        }

    }

    internal class BackgroundWorkerContext(int threadIndex, JobEngine engine)
    {
        internal int ThreadIndex { get; init; } = threadIndex;
        internal JobEngine Engine { get; init; } = engine;
        internal AutoResetEvent Notification { get; init; } = new AutoResetEvent(false);
        internal bool MustTerminate { get; set; } = false;
    }

    public class JobEngine
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
        private int contextRoundRobin = 0;
        private readonly int workerCount;
        private readonly Thread[] workers;
        private BackgroundWorkerContext[] contexts;
        private List<BackgroundJob> jobs = new List<BackgroundJob>();

        public void Run(BackgroundJob job)
        {
            lock (contextLock)
                jobs.Add(job);

            NotifyWorkers();
        }

        private void NotifyWorkers()
        {
            for (int i = 0; i < workerCount; i++)
                contexts[i].Notification.Set();
        }

        private bool FindNextJobItem([MaybeNullWhen(false)] out BackgroundJob? job, [MaybeNullWhen(false)] out BackgroundJobItem? item)
        {
            lock (contextLock)
            {
                int numJobs = jobs.Count;

                if (numJobs > 0)
                {
                    for (int i = 0; i < numJobs; i++)
                    {
                        int idx = (i + contextRoundRobin) % numJobs;
                        if (jobs[idx].FindNextJobItem(out item))
                        {
                            job = jobs[idx];
                            contextRoundRobin = (idx + 1) % numJobs;
                            return true;
                        }
                    }
                }
            }

            job = null;
            item = null;
            return false;
        }

        private void JobStatusChanged(BackgroundJob job)
        {
        }

        private bool TryExecuteJob()
        {
            if (FindNextJobItem(out BackgroundJob? job, out BackgroundJobItem? item) &&
                item is not null && 
                job is not null)
            {
                bool executedSuccessfully = item.Execute(job);
                if (!executedSuccessfully && item.FailureCancelsJob)
                    job.IsCancelled = true;

                JobStatusChanged(job);

                return executedSuccessfully;
            }

            return false;
        }

       /*public async void TerminateAll()
        {
            foreach (BackgroundWorkerContext ctx in contexts)
            {
                ctx.MustTerminate = true;
                ctx.Notification.Set();
            }

            // TODO
        }*/

        private static void BackgroundWorkerProc(object? p)
        {
            if (p is not BackgroundWorkerContext ctx)
                throw new ArgumentException(nameof(p));

            while (true)
            {
                while (ctx.Engine.TryExecuteJob())
                {
                    if (ctx.MustTerminate) return;
                }

                ctx.Notification.WaitOne();

                if (ctx.MustTerminate) 
                    return;
            }
        }
    }
}
