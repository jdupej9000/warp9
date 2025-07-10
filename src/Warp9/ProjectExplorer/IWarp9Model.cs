using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Jobs;

namespace Warp9.ProjectExplorer
{
    public interface IWarp9Model
    {
        public void StartJob(IEnumerable<IJobItem> items, string? title=null);
    }
}
