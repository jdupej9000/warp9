using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void JobItemDone(long key, JobItemStatus status);
    }

    public class BackgroundJobItem
    {
        public long Key { get; init; }
        public string Title { get; init; }
        public bool FailureCancelsJob { get; init; }

        public void Execute(IJobContext ctx)
        {
        }
    }

    public class BackgroundJob
    {
        
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

        private readonly int workerCount;
        private readonly Thread[] workers;
        private BackgroundWorkerContext[] contexts;

        private bool TryExecuteJob()
        {
            return false;
        }

        public async void TerminateAll()
        {
            foreach (BackgroundWorkerContext ctx in contexts)
            {
                ctx.MustTerminate = true;
                ctx.Notification.Set();
            }

            // TODO
        }

        private static void BackgroundWorkerProc(object? p)
        {
            if (p is not BackgroundWorkerContext ctx)
                throw new ArgumentException();

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
