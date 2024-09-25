using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Warp9.Model
{
    public enum ProjectEntryKind
    {
        Specimens = 0,
        MeshCorrespondence = 1,
        MeshPca = 2,
        Gallery = 3,

        Invalid = -1
    };

    public class ProjectEntry
    {
        [JsonIgnore]
        public int Id { get; set; } = -1;

        public string Name { get; set; } = string.Empty;
        public ProjectEntryKind Kind { get; set; } = ProjectEntryKind.Invalid;
        public List<int> Refs { get; set; } = new List<int>();
        public List<int> Deps { get; set; } = new List<int>();
        public ProjectEntryPayload Payload { get; set; } = ProjectEntryPayload.Empty;
    }
}
