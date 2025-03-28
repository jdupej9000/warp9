﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        public ProjectEntry(long id, ProjectEntryKind kind)
        {
            Id = id;
            Kind = kind;
        }

        [JsonIgnore]
        public long Id { get; set; } = -1;

        [JsonPropertyName("id")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("kind")]
        public ProjectEntryKind Kind { get; set; } = ProjectEntryKind.Invalid;

        [JsonPropertyName("refs")]
        public List<int> Refs { get; set; } = new List<int>();

        [JsonPropertyName("deps")]
        public List<long> Deps { get; set; } = new List<long>();

        [JsonPropertyName("payload")]
        public ProjectEntryPayload Payload { get; set; } = ProjectEntryPayload.Empty;
    }
}
