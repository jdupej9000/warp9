using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Jobs;

namespace Warp9.JobItems
{
    public class CopyJobItem : ProjectJobItem
    {
        public CopyJobItem(int index, string sourceItem, string destItem) :
           base(index, "Copy items", JobItemFlags.RunsAlone)
        {
            SourceItem = sourceItem;
            DestItem = destItem;
        }

        public string SourceItem { get; init; }
        public string DestItem { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            return ctx.Workspace.TryCopy(SourceItem, DestItem);
        }
    }
}
