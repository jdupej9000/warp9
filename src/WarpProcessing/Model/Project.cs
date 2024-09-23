using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class Project
    {
        private Project(string path, ZipArchive archv)
        {
            filePath = path;
            archive = archv;
        }

        string filePath;
        ZipArchive? archive;
        ProjectManifest? manifest;
        Dictionary<string, int> archiveIndex = new Dictionary<string, int>();

        public bool IsOpen => archive is not null;

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
            if (manifest is null || archive is null) 
                throw new InvalidOperationException();

            if (!manifest.References.TryGetValue(index, out ProjectReference? refInfo))
                throw new InvalidOperationException();

            if (!archiveIndex.TryGetValue(refInfo.FileName, out int refFileIndex))
                throw new InvalidDataException();

            return archive.Entries[refFileIndex].Open();
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
