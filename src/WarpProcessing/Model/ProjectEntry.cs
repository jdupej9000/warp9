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
        public ProjectEntry(int id, ProjectEntryKind kind)
        {
            Id = id;
            Kind = kind;
        }

        [JsonIgnore]
        public int Id { get; set; } = -1;

        [JsonPropertyName("id")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("kind")]
        public ProjectEntryKind Kind { get; set; } = ProjectEntryKind.Invalid;

        [JsonPropertyName("refs")]
        public List<int> Refs { get; set; } = new List<int>();

        [JsonPropertyName("deps")]
        public List<int> Deps { get; set; } = new List<int>();

        [JsonPropertyName("payload")]
        public ProjectEntryPayload Payload { get; set; } = ProjectEntryPayload.Empty;
    }
}
