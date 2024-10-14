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
        public BackgroundJobItem(long key, string title, bool failIsFatal = false)
        {
            Key = key;
            Title = title;
            FailureCancelsJob = failIsFatal;
        }

        public long Key { get; init; }
        public string Title { get; init; }
        public bool FailureCancelsJob { get; init; }

        public bool Execute(IJobContext ctx)
        {
            return false;
        }
    }

    public class BackgroundJob : IJobContext, INotifyPropertyChanged
    {
        public BackgroundJob(string title)
        {
            Title = title;
        }

        private int numDone = 0, numError = 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title { get; init; }
        public IJobLog Log => throw new NotImplementedException();
        public bool IsCancelled { get; set; } = false;

        public int NumItems => Items.Count;
        public int NumItemsDone => numDone;
        public int NumErrors => numError;
        public string Status => MakeStatus();

        public List<BackgroundJobItem> Items { get; init; } = new List<BackgroundJobItem>();

        public bool FindNextJobItem([MaybeNullWhen(false)] out BackgroundJobItem? item)
        {
            item = null;
            return false;
        }

        public void JobItemDone(long key, JobItemStatus status)
        {
            numDone++;

            if (status == JobItemStatus.Failed)
                numError++;

            Notify();
        }

        private string MakeStatus()
        {
            if (IsCancelled)
                return "Canceled";
            else if (numError > 0)
                return string.Format("{0}/{1} done, {2} errors", NumItemsDone, NumItems, NumErrors);
            else 
                return string.Format("{0}/{1} done", NumItemsDone, NumItems);
        }

        private void Notify()
        {
            if (PropertyChanged is not null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(NumItemsDone)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(NumErrors)));
            }
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

            BackgroundJob job = new BackgroundJob("CPD-DCA");
            job.Items.Add(new BackgroundJobItem(0, "Initialize CPD"));
            job.Items.Add(new BackgroundJobItem(1, "Register mesh 1"));
            jobs.Add(job);
            jobs.Add(job);
        }

        private object contextLock = new object();
        private int contextRoundRobin = 0;
        private readonly int workerCount;
        private readonly Thread[] workers;
        private BackgroundWorkerContext[] contexts;
        private ObservableCollection<BackgroundJob> jobs = new ObservableCollection<BackgroundJob>();

        public ObservableCollection<BackgroundJob> Jobs => jobs;

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
            TerminateAll();
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
