using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Jobs
{
    public enum JobItemStatus
    {
        Waiting,
        Running,
        Completed,
        Failed
    }

    [Flags]
    public enum JobItemFlags
    {
        None = 0x0,
        FailuesAreFatal = 0x1,
        WaitsForAllPrevious = 0x2,
        BlocksNext = 0x4,
        RunsAlone = WaitsForAllPrevious | BlocksNext
    }

    public interface IJobItem
    {
        public string Title { get; }
        public JobItemFlags Flags { get; }
        public JobItemStatus Run(IJob job, IJobContext ctx);
    }
}
