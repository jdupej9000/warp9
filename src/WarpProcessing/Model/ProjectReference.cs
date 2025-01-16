using System;
using System.IO;
using System.Text.Json.Serialization;

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
        PlyMesh = 8,

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

        public static ProjectReferenceInfo CreateInternal(string fileName, ProjectReferenceFormat fmt)
        {
            return new ProjectReferenceInfo()
            {
                FileName = fileName,
                Format = fmt,
                IsInternal = true
            };
        }

        public ProjectReferenceInfo WithRelativePath(string workingDir)
        {
            if (IsInternal)
                throw new InvalidOperationException("Only external references are allowed.");

            if (!Path.IsPathRooted(FileName))
                return this;

            return new ProjectReferenceInfo()
            {
                FileName = Path.GetRelativePath(workingDir, FileName),
                Format = Format,
                IsInternal = false
            };
        }

        public ProjectReferenceInfo WithAbsolutePath(string workingDir)
        {
            if (IsInternal)
                throw new InvalidOperationException("Only external references are allowed.");

            if (Path.IsPathRooted(FileName))
                return this;

            return new ProjectReferenceInfo()
            {
                FileName = Path.Combine(workingDir, FileName),
                Format = Format,
                IsInternal = false
            };
        }
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
