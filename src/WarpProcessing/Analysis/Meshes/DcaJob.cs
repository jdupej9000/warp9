using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Jobs;

namespace Warp9.Analysis.Meshes
{
    public class DcaJob : IJob
    {
        public JobItemStatus ExecuteItem(IJobItem item)
        {
            if (item is DcaJobItem ji)
                return ji.Run(this);
            else
                return JobItemStatus.Failed;
        }

        public IEnumerable<IJobItem> GenerateItems()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class DcaJobItem : IJobItem
    {
        public string Title => throw new NotImplementedException();
        public JobItemFlags Flags => throw new NotImplementedException();

        public JobItemStatus Run(IJob job)
        {
            if (job is not DcaJob dca)
                return JobItemStatus.Failed;

            return RunInternal(dca) ? JobItemStatus.Completed : JobItemStatus.Failed;
        }

        protected virtual bool RunInternal(DcaJob dca)
        {
            return false;
        }
    }

    public class DcaRigidPreRegistrationJobItem : DcaJobItem
    {
    }

    public class DcaNonrigidInitializationJobItem : DcaJobItem
    {
    }

    public class DcaNonrigidRegistrationJobItem : DcaJobItem
    {
    }

    public class DcaSurfaceProjectionJobItem : DcaJobItem
    {
    }

    public class DcaRigidPostRegistrationJobItem : DcaJobItem
    {
    }
}
