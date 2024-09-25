using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class Warp9ProjectArchive : IProjectArchive
    {
        public Warp9ProjectArchive(string fileName, bool canWrite)
        {
            this.canWrite = canWrite;
            this.fileName = fileName;
            workingDir = Path.GetDirectoryName(fileName) ?? throw new InvalidOperationException();

            if (canWrite)
            {
                archive = ZipFile.Open(fileName, ZipArchiveMode.Create);
            }
            else
            {
                archive = ZipFile.OpenRead(fileName);
                InventoryContents();
            }
        }

        bool canWrite;
        string fileName;
        string workingDir;
        ZipArchive? archive;
        readonly Dictionary<string, int> archiveIndex = new Dictionary<string, int>();

        public bool IsOpen => archive is not null;
        public string WorkingDirectory => workingDir;

        public void Close()
        {
            if (archive is not null)
            {
                archive.Dispose();
                archive = null;
            }
        }

        public bool ContainsFile(string name)
        {
            return archiveIndex.ContainsKey(name);
        }

        public Stream CreateFile(string name)
        {
            if (!canWrite)
                throw new InvalidOperationException("This archive is read-only.");

            if (archive is null)
                throw new InvalidOperationException("The archive is not open.");

            if (archiveIndex.ContainsKey(name))
                throw new InvalidOperationException("That file already exists in the archive.");

            archiveIndex.Add(name, archive.Entries.Count);

            bool isManifest = name == Project.ManifestFileName;
            ZipArchiveEntry entry = archive.CreateEntry(name, isManifest ? CompressionLevel.SmallestSize : CompressionLevel.NoCompression);
            return entry.Open();
        }

        public void CopyFileFrom(string name, IProjectArchive other)
        {
            using Stream srcStream = other.OpenFile(name);
            using Stream destStream = CreateFile(name);
            srcStream.CopyTo(destStream);
        }

        public Stream OpenFile(string name)
        {
            if (archive is null)
                throw new InvalidOperationException("The archive is not open.");

            if (!archiveIndex.TryGetValue(name, out int zipEntryIndex))
                throw new InvalidOperationException("The archive does not contain this entry.");

            return archive.Entries[zipEntryIndex].Open();
        }

        private void InventoryContents()
        {
            if (archive is null)
                throw new InvalidOperationException();

            archiveIndex.Clear();
            for (int i = 0; i < archive.Entries.Count; i++)
                archiveIndex.Add(archive.Entries[i].Name, i);
        }

        public void Dispose()
        {
            Close();
        }
    }
}
