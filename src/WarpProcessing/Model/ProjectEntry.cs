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
        Comment,
        Specimens,
        Dca
    };

    public class ProjectEntry
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; }
        public ProjectEntryKind Kind { get; set; }
        public List<int> Refs { get; set; }
        public List<int> Deps { get; set; }
        public ProjectEntryPayload Payload { get; set; }
    }
}
