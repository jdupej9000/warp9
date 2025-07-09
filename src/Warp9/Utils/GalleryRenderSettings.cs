using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Viewer;

namespace Warp9.Utils
{
    public class GalleryRenderSettings
    {
        // Render settings
        public string Directory { get; set; } = string.Empty;
        public int FormatIndex { get; set; } = 0;
        public IEnumerable<string> FormatList => Formats;
        public int AdapterIndex { get; set; } = 0;
        public IEnumerable<string> AdapterList => HeadlessRenderer.EnumAdapters().Values;
        public bool OverwriteExistingFiles { get; set; } = true;
        public bool Autocrop { get; set; } = false;


        // Mods
        public bool ModResolution { get; set; } = false;
        public int ModResolutionWidth { get; set; } = 4096;
        public int ModResolutionHeight { get; set;} = 4096;
        public bool ModResolutionAspect { get; set; } = false;

        public bool ModBackground {get; set; } = true;
        public int ModBackgroundIndex { get; set; } = 0;
        public IEnumerable<string> ModBackgroundList => BackgroundColors.Select((t) => t.Item1);

        public bool ModView { get; set; } = false;
        public int ModViewIndex { get; set; } = 0;
        public List<string> ModViewList { get; set; } = new List<string>();

        public bool ModDisableGrid { get; set; } = true;


        static List<(string, Color)> BackgroundColors = new List<(string, Color)>
        {
            ("Transparent", Color.Transparent),
            ("White", Color.White),
            ("Black", Color.Black)
        };

        static List<string> Formats = new List<string>()
        {
            "PNG"
        };
    }
}
