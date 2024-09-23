using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class ProjectManifest
    {
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;
        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public Dictionary<int, ProjectEntry> Entries { get; set; } = new Dictionary<int, ProjectEntry>();
        public Dictionary<int, ProjectReferenceInfo> References { get; set; } = new Dictionary<int, ProjectReferenceInfo>();
    }
}
