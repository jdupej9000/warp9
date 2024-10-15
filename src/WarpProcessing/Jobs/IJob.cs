using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Jobs
{
    public interface IJob
    {
        public JobItemStatus ExecuteItem(IJobItem item);
    }
}
