using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Policy;
using System.Threading;

namespace Warp9.Jobs
{
    internal class BackgroundWorkerContext(int threadIndex, JobEngine engine)
    {
        internal int ThreadIndex { get; init; } = threadIndex;
        internal JobEngine Engine { get; init; } = engine;
        internal AutoResetEvent Notification { get; init; } = new AutoResetEvent(false);
        internal bool MustTerminate { get; set; } = false;
    }

    public record JobEngineProgress
    {
        public JobEngineProgress()
        {
        }

        public JobEngineProgress(IJob cur, int numJobs=0)
        {
            IsBusy = true;
            NumJobsQueued = numJobs;
            NumItems = cur.NumItems;
            NumItemsDone = cur.NumItemsDone;
            NumItemsFailed = cur.NumItemsFailed;
            CurrentItemText = cur.Title + ": " + cur.StatusText;
        }

        public bool IsBusy { get; private init; } = false;
        public int NumJobsQueued { get; private init; } = 0;
        public int NumItemsDone { get; private init; } = 0;
        public int NumItemsFailed { get; private init; } = 0;
        public int NumItems { get; private init; } = 0;
        public string CurrentItemText { get; private init; } = string.Empty;
    }

    public class JobEngine : IDisposable
    {
        public JobEngine()
        {
            workerCount = 1;// Environment.ProcessorCount;

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
        private Queue<IJob> jobs = new Queue<IJob>();
        public event EventHandler<JobEngineProgress>? ProgressChanged;

        public IJob? CurrentJob
        {
            get
            {
                lock (contextLock)
                {
                    if(jobs.TryPeek(out IJob? job))
                        return job;

                    return null;
                }
            }
        }

        public void Run(IJob job)
        {
            lock (contextLock)
                jobs.Enqueue(job);

            NotifyWorkers();
        }

        public static IJobContext RunImmediately(IJob job)
        {
            IJobContext ctx = job.Context ?? throw new ArgumentNullException(nameof(job));
            while (!job.IsCompleted && job.TryExecuteNext()) ;
            WriteCompletionToLog(job);
            return ctx;
        }

        public void Dispose()
        {
            TerminateAll();
        }



        private void TerminateAll()
        {
            for (int i = 0; i < workerCount; i++)
                contexts[i].MustTerminate = true;

            NotifyWorkers();
        }

        private void NotifyWorkers()
        {
            for (int i = 0; i < workerCount; i++)
                contexts[i].Notification.Set();
        }

        private void UpdateProgress()
        {
            IJob? job = CurrentJob;

            if (job is not null)
            {
                ProgressChanged?.Invoke(this, new JobEngineProgress(job));
            }
            else
            {
                ProgressChanged?.Invoke(this, new JobEngineProgress());
            }
        }

        private void JobItemDone()
        {
            IJob? job = CurrentJob;
            if (job is not null)
            {
                if (job.IsCompleted)
                    jobs.Dequeue();
            }

            UpdateProgress();
            NotifyWorkers();
        }

        private static void WriteCompletionToLog(IJob job)
        {
            if (job.Context is null)
                return;

            job.Context.WriteLog(-1, MessageKind.Information,
                string.Format("{0} job items queued, {1} succeeded, {2} failed.", job.NumItems, job.NumItemsDone, job.NumItemsFailed));

            if (job.IsFatallyFailed)
                job.Context.WriteLog(-1, MessageKind.Error, "The job has FAILED.");
            else
                job.Context.WriteLog(-1, MessageKind.Information, "The job has finished successfully.");
        }

        private static void BackgroundWorkerProc(object? p)
        {
            if (p is not BackgroundWorkerContext ctx)
                throw new ArgumentException(nameof(p));

            IJob? currentJob;

            while (true)
            {
                currentJob = ctx.Engine.CurrentJob;
                if (ctx.MustTerminate)
                    return;

                if (currentJob != null)
                {
                    while (true)
                    {
                        if (currentJob.TryExecuteNext())
                            ctx.Engine.JobItemDone();
                        else
                            break;

                        if (ctx.MustTerminate)
                            return;
                    }
                }

                if (ctx.MustTerminate)
                    return;

                ctx.Notification.WaitOne();
            }
        }
    }
}