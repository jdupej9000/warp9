using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            manifest = new ProjectManifest();
        }

        private Project(string path, ZipArchive archv)
        {
            filePath = path;
            archive = archv;
        }

        string filePath;
        ZipArchive? archive;
        ProjectManifest? manifest;
        readonly Dictionary<string, int> archiveIndex = new Dictionary<string, int>();
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

        public Stream ReadReference(int index)
        {
            if (manifest is null) 
                throw new InvalidOperationException();

            if (!manifest.References.TryGetValue(index, out ProjectReference? refInfo))
                throw new InvalidOperationException("Invalid reference Id.");

            if (newFiles.TryGetValue(index, out byte[]? rawData))
                return new MemoryStream(rawData, false);

            if (archive is not null && archiveIndex.TryGetValue(refInfo.FileName, out int refFileIndex))
                return archive.Entries[refFileIndex].Open();

            throw new InvalidDataException("Archive references a nonexistent file.");
        }

        public int AddReference(string fileName, ProjectReferenceFormat fmt, byte[] payload)
        {
            if (archiveIndex.ContainsKey(fileName))
                throw new InvalidOperationException("That file is already part of the archive.");

            throw new NotImplementedException();
        }

        public int TryFindReference(string fileName)
        {
            if (archiveIndex.TryGetValue(fileName, out int refFileIndex))
                return refFileIndex;

            return -1;
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

            manifest = JsonSerializer.Deserialize<ProjectManifest>(s, opts);
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
