using System;
using System.Collections.Generic;
using System.Text;
using Warp9.Model;

namespace Warp9Cli.Cli
{
    public class CommandExecutionContext : IDisposable
    {
        public Warp9ProjectArchive? ProjectArchive { get; set; }
        public Project? Project { get; set; }

        public void Dispose()
        {
            if(ProjectArchive is not null)
                ProjectArchive.Dispose();

            if(Project is not null)
                Project.Dispose();
        }
    }
}
