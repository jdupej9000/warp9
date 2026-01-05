using System;
using System.Collections.Generic;
using System.Text;
using Warp9.JobItems;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public static class DiffMatrixJob
    {
        public static IEnumerable<ProjectJobItem> Create(DiffMatrixConfiguration cfg, Project proj)
        {
            int index = 0;
            yield return new DiffMatrixJobItem(index++, cfg.ResultEntityName, cfg);
        }
    }
}
