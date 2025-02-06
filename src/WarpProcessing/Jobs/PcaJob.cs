using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.JobItems;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public static class PcaJob
    {
        public static IEnumerable<ProjectJobItem> CreateDcaPca(PcaConfiguration cfg, Project proj)
        {
            int index = 0;
            yield return new DcaPcaJobItem(index++, cfg.ResultEntityName, cfg);
        }
    }
}
