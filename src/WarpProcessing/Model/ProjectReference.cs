using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public enum ProjectReferenceFormat
    {
        ObjMesh = 1,
        W9Mesh = 2,
        W9Pcl = 3,
        PngImage = 4,
        JpegImage = 5,
        W9Matrix = 6,
        MorphoLandmarks = 7,

        Invalid = int.MaxValue
    }

    public class ProjectReferenceInfo
    {
        [JsonPropertyName("file")]
        public required string FileName { get; set; }

        [JsonPropertyName("fmt")]
        public ProjectReferenceFormat Format { get; set; }

        [JsonPropertyName("intern")]
        public bool IsInternal { get; set; }
    }

    public class ProjectReference
    {
        public ProjectReference(long id, ProjectReferenceInfo info, object? nat=null)
        {
            Id = id;
            Info = info;
            NativeObject = nat;
        }

        public long Id { get; set; }
        public ProjectReferenceInfo Info { get; set; }
        public bool HasNativeObject => NativeObject is not null;
        public object? NativeObject { get; set; }
        
    }
}
