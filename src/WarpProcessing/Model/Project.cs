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

            return ret;
        }
    }
}
