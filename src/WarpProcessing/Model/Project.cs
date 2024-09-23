using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class Project : IDisposable
    {
        private Project()
        {
            filePath = string.Empty;
            archive = null;
            workingDir = null;
        }

        private Project(string path, ZipArchive archv)
        {
            filePath = path;
            workingDir = Path.GetDirectoryName(path) ?? throw new InvalidOperationException();
            archive = archv;
        }

        string filePath;
        string? workingDir;
        ZipArchive? archive;
        readonly Dictionary<string, int> archiveIndex = new Dictionary<string, int>();
        readonly Dictionary<int, ProjectReference> references = new Dictionary<int, ProjectReference>();
        readonly Dictionary<int, byte[]> newFiles = new Dictionary<int, byte[]>();

        private static readonly string ManifestFileName = "manifest.json";

        public void Close()
        {
            if (archive is not null)
            {
                archive.Dispose();
                archive = null;
            }
        }

        public bool TryGetReference<T>(int index, [MaybeNullWhen(false)] out T value)
        {
            if (!references.TryGetValue(index, out ProjectReference? reference))
            {
                value = default;
                return false;
            }

            if (reference.HasNativeObject)
            {
                if (reference.NativeObject is T nativeObject)
                {
                    value = nativeObject;
                    return true;
                }
               
                throw new InvalidOperationException();
            }

            if (reference.Info.IsInternal)
            {
                if (archive is null)
                {
                    value = default;
                    return false;
                }

                if (archiveIndex.TryGetValue(reference.Info.FileName, out int fileIndexZip))
                {
                    using Stream zipEntryStream = archive.Entries[fileIndexZip].Open();
                    return CodecBank.ProjectCodecs.TryDecode<T>(zipEntryStream, reference.Info.Format, null, out value);
                }

                value = default;
                return false;
            }

            string externalPath = reference.Info.FileName;
            if (!Path.IsPathRooted(externalPath))
            {
                if (workingDir is null)
                    throw new InvalidOperationException();

                externalPath = Path.Combine(workingDir, externalPath);
            }

            try
            {
                using FileStream fileStream = new FileStream(externalPath, FileMode.Open, FileAccess.Read);
                return CodecBank.ProjectCodecs.TryDecode<T>(fileStream, reference.Info.Format, null, out value);
            }
            catch (FileNotFoundException)
            {
                value = default;
                return false;
            }
        }

        public bool TryAddReferenceDirect<T>(string fileName, ProjectReferenceFormat fmt, T val, int index)
        {
            index = 0; // TODO
            ProjectReference reference = new ProjectReference(index,
                 new ProjectReferenceInfo() { FileName = fileName, Format = fmt, IsInternal = true }, val);
            references.Add(index, reference);
            return true;
        }

        public bool TryAddReferenceExternal(string fileName, ProjectReferenceFormat fmt, int index)
        {
            string fileNameMinimal = fileName;

            if (workingDir is not null)
                Path.GetRelativePath(workingDir, fileName);

            index = 0; // TODO
            ProjectReference reference = new ProjectReference(index,
                new ProjectReferenceInfo() { FileName = fileNameMinimal, Format = fmt, IsInternal = false });
           
            references.Add(index, reference);
            return true;
        }


        private void MakeArchiveIndex()
        {
            if (archive is null)
                throw new InvalidOperationException();

            archiveIndex.Clear();
            for (int i = 0; i < archive.Entries.Count; i++)
                archiveIndex.Add(archive.Entries[i].Name, i);
        }

        private void LoadManifest()
        {
            if (archive is null) 
                throw new InvalidOperationException();

            ZipArchiveEntry manifestEntry = archive.Entries.First((e) => e.Name == ManifestFileName);

            using Stream manifestStream = manifestEntry.Open();
            ParseRawManifest(manifestStream);
        }

        private void ParseRawManifest(Stream s)
        {
            JsonSerializerOptions opts = new JsonSerializerOptions();
            opts.AllowTrailingCommas = false;

            ProjectManifest? manifest = JsonSerializer.Deserialize<ProjectManifest>(s, opts);
            if (manifest is null)
                throw new InvalidDataException();

            foreach (var kvp in manifest.References)
                references.Add(kvp.Key, new ProjectReference(kvp.Key, kvp.Value));
        }

        public void Dispose()
        {
            Close();
        }

        public static Project CreateEmpty()
        {
            return new Project();
        }

        public static Project Load(string path, bool keepOpen=true)
        {
            ZipArchive zip = ZipFile.OpenRead(path);
            
            Project ret = new Project(path, zip);
            ret.LoadManifest();

            if (!keepOpen)
                ret.Close();

            ret.MakeArchiveIndex();

            return ret;
        }
    }
}
