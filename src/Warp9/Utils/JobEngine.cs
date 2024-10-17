using SharpDX.Direct3D11;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Media3D;
using Warp9.Jobs;

namespace Warp9.Utils
{

    public class BackgroundJob : INotifyPropertyChanged
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

        private string MakeStatus()
        {
            if (false)
                return "Canceled";
            else if (Job.NumItemsFailed > 0)
                return string.Format("{0}/{1} done, {2} errors", Job.NumItemsDone, Job.NumItems, Job.NumItemsFailed);
            else 
                return string.Format("{0}/{1} done", Job.NumItemsDone, Job.NumItems);
        }

    }

    internal class BackgroundWorkerContext(int threadIndex, JobEngine engine)
    {
        internal int ThreadIndex { get; init; } = threadIndex;
        internal JobEngine Engine { get; init; } = engine;
        internal AutoResetEvent Notification { get; init; } = new AutoResetEvent(false);
        internal bool MustTerminate { get; set; } = false;
    }

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
        private int contextRoundRobin = 0;
        private readonly int workerCount;
        private readonly Thread[] workers;
        private BackgroundWorkerContext[] contexts;
        private ObservableCollection<BackgroundJob> jobs = new ObservableCollection<BackgroundJob>();
        private ObservableCollection<BackgroundJob> finishedJobs = new ObservableCollection<BackgroundJob>();

        public ObservableCollection<BackgroundJob> Jobs => jobs;
        public ObservableCollection<BackgroundJob> FinishedJobs => finishedJobs;

        public void Run(BackgroundJob job)
        {
            lock (contextLock)
                jobs.Add(job);

            NotifyWorkers();
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

        private void JobStatusChanged(BackgroundJob job)
        {
            lock (contextLock)
            {
                if (job.IsDone)
                {
                    job.Context = null;
                    finishedJobs.Add(job);
                    jobs.Remove(job);
                }
            }

            NotifyWorkers();
        }

        private BackgroundJob? GetNextJob(ref int idx)
        {
            lock (contextLock)
            {
                if (idx >= jobs.Count)
                    idx = 0;

                if(idx < jobs.Count)
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
                BackgroundJob? firstBlockedJob = null;
                BackgroundJob? nextJob = ctx.Engine.GetNextJob(ref jobIndex);
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
                    else if(itemTaken)
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
