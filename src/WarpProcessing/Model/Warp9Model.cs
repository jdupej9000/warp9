using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class Warp9Model
    {
        public Warp9Model(Project project)
        {
            Project = project;
        }

        public Project Project { get; init; }
        public bool IsDirty { get; private set; } = false;
    }
}
