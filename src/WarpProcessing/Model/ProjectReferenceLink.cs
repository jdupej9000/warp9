using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public readonly struct ProjectReferenceLink
    {
        public ProjectReferenceLink(int idx)
        {
            ReferenceIndex = idx;
        }

        [JsonPropertyName("ref")]
        public int ReferenceIndex { get; private init; }
    }
}
