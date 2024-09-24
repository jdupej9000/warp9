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
        ObjMesh,
        W9Mesh,
        PngImage,
        JpegImage,
        FloatMatrix
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
        public ProjectReference(int id, ProjectReferenceInfo info, object? nat=null)
        {
            Id = id;
            Info = info;
            NativeObject = nat;
        }

        public int Id { get; set; }
        public ProjectReferenceInfo Info { get; set; }
        public bool HasNativeObject => NativeObject is not null;
        public object? NativeObject { get; set; }
        
    }
}
