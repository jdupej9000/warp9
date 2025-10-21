using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.JobItems;
using Warp9.Jobs;
using Warp9.Model;

namespace Warp9.Test
{
    abstract class TestJobItem : ProjectJobItem
    {
        public TestJobItem(int index, string name, List<int> dest, JobItemFlags flags) :
            base(index, name, flags)
        {
            result = dest;
        }

        protected List<int> result;

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            lock (result)
            {
                Console.WriteLine($"Adding {ItemIndex}.");
                result.Add(ItemIndex);
            }
            return true;
        }
    }

    class ParallelJobItem : TestJobItem
    {
        public ParallelJobItem(int index, List<int> dest) :
            base(index, "parallel", dest, JobItemFlags.None)
        { }     
    }

    class SerialJobItem : TestJobItem
    {
        public SerialJobItem(int index, List<int> dest) :
            base(index, "serial", dest, JobItemFlags.RunsAlone)
        { }
    }

    class BarrierJobItem : TestJobItem
    {
        public BarrierJobItem(int index, List<int> dest) :
            base(index, "barrier", dest, JobItemFlags.RunsAlone | JobItemFlags.WaitsForAllPrevious)
        { }
    }


    [TestClass]
    public class JobEngineTests
    {
        static void ExecuteWait(int numThreads, params TestJobItem[] items)
        {
            Project proj = Project.CreateEmpty();

            JobEngine je = new JobEngine(numThreads);
            ProjectJobContext ctx = new ProjectJobContext(proj);

            Job job = Job.Create(items, ctx, "");
            je.ProgressChanged += (sender, args) =>
            {
                if (args.NumItems == args.NumItemsFailed + args.NumItemsDone)
                {
                    Console.WriteLine("All done, disposing...");
                    je.Dispose();
                }
            };

            je.Run(job);
            Assert.IsTrue(je.WaitForWorkerTermination(new TimeSpan(0, 0, 1)));
        }

        static void AssertList(List<int> list, params int[][] permutables)
        {
            int[] arr = list.ToArray();

            int offset = 0;
            foreach (int[] seg in permutables)
            {
                int n = seg.Length;
                for (int i = 0; i < n; i++)
                    Assert.IsTrue(arr.AsSpan(offset, n).Contains(seg[i]));

                offset += n;
            }
        }

        const int NumPasses = 1;

        [TestMethod]
        [DoNotParallelize]
        public void TerminateTest()
        {
            JobEngine je = new JobEngine(4);

            je.Dispose();
            Assert.IsTrue(je.WaitForWorkerTermination(new TimeSpan(0, 0, 1)));
        }

        [TestMethod]
        [DoNotParallelize]
        public void SerialTest()
        {
            for (int i = 0; i < NumPasses; i++)
            {
                List<int> x = new List<int>();
                ExecuteWait(4,
                    new SerialJobItem(0, x),
                    new SerialJobItem(1, x),
                    new SerialJobItem(2, x),
                    new SerialJobItem(3, x));

                AssertList(x,
                    new int[] { 0 },
                    new int[] { 1 },
                    new int[] { 2 },
                    new int[] { 3 });
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void ParallelTest()
        {
            for (int i = 0; i < NumPasses; i++)
            {
                List<int> x = new List<int>();
                ExecuteWait(4,
                    new ParallelJobItem(0, x),
                    new ParallelJobItem(1, x),
                    new ParallelJobItem(2, x),
                    new ParallelJobItem(3, x));

                AssertList(x,
                    new int[] { 0, 1, 2, 3 });
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void BarrierTest()
        {
            for (int i = 0; i < NumPasses; i++)
            {
                List<int> x = new List<int>();
                ExecuteWait(4,
                    new ParallelJobItem(0, x),
                    new ParallelJobItem(1, x),
                    new ParallelJobItem(2, x),
                    new ParallelJobItem(3, x),
                    new BarrierJobItem(4, x),
                    new ParallelJobItem(5, x),
                    new ParallelJobItem(6, x),
                    new ParallelJobItem(7, x),
                    new ParallelJobItem(8, x));

                AssertList(x,
                    new int[] { 0, 1, 2, 3 },
                    new int[] { 4 },
                    new int[] { 5, 6, 7, 8 });

            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void ParallelSerialTest()
        {
            for (int i = 0; i < NumPasses; i++)
            {
                List<int> x = new List<int>();
                ExecuteWait(4,
                    new ParallelJobItem(0, x),
                    new ParallelJobItem(1, x),
                    new ParallelJobItem(2, x),
                    new ParallelJobItem(3, x),
                    new SerialJobItem(4, x),
                    new SerialJobItem(5, x),
                    new ParallelJobItem(6, x),
                    new ParallelJobItem(7, x),
                    new ParallelJobItem(8, x));

                AssertList(x,
                    new int[] { 0, 1, 2, 3 },
                    new int[] { 4 },
                    new int[] { 5 },
                    new int[] { 6, 7, 8 });

            }
        }
    }
}
