using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Model;
using Warp9.Viewer;

namespace Warp9.Scene
{
    public class GridSceneElement : ISceneElement
    {
        [JsonPropertyName("visible")]
        public bool Visible { get; set; } = true;

        [JsonIgnore]
        public RenderItemVersion Version { get; } = new RenderItemVersion();

        public void ConfigureRenderItem(RenderItemDelta delta, Project proj, RenderItemBase rib)
        {
            if (rib is not RenderItemGrid ri)
                return;

            ri.Visible = Visible;

            ri.Version.Commit(delta);
        }
    }
}
