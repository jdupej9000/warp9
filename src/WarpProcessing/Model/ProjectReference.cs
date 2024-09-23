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

    public class ProjectReference
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string FileName { get; set; }
        public ProjectReferenceFormat Format { get; set; }

        
    }
}
