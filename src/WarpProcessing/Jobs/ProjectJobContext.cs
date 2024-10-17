using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;

namespace Warp9.Jobs
{
    public class ProjectJobContext : IJobContext
    {
        public ProjectJobContext(Project proj)
        {
            Project = proj;
        }

        public JobWorkspace Workspace { get; init; } = new JobWorkspace();
        public Project Project { get; init; }
    }
}
