using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Warp9.Utils;

namespace Warp9.Model
{
    class W9ArchiveOpenFile
    {
        public W9ArchiveOpenFile(byte[] data)
        {
            Data = data;
            OpenCount = 1;
        }

        public byte[] Data { get; init; }
        public int OpenCount { get; set; }

        public byte[] AddRef()
        {
            OpenCount++;
            return Data;
        }

        public bool RemoveRef()
        {
            OpenCount--;
            return OpenCount == 0;
        }
    }

    public class Warp9ProjectArchive : IProjectArchive, IMemoryStreamNotificationSink
    {
        public Warp9ProjectArchive(string fileName, bool canWrite, bool fixMultithread=true)
        {
            this.canWrite = canWrite;
            this.fileName = fileName;
            multithreadingWorkaround = fixMultithread;
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

        bool multithreadingWorkaround;
        bool canWrite;
        string fileName;
        string workingDir;
        ZipArchive? archive;
        readonly Dictionary<string, int> archiveIndex = new Dictionary<string, int>();
        readonly Dictionary<string, W9ArchiveOpenFile> openFiles = new Dictionary<string, W9ArchiveOpenFile>();

        public bool IsOpen => archive is not null;
        public string WorkingDirectory => workingDir;
        public string? FileName => fileName;

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

            //archiveIndex.Add(name, archive.Entries.Count);

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


            if (multithreadingWorkaround)
            {
                return OpenInternal(name);
            }
            else
            {       
                if (!archiveIndex.TryGetValue(name, out int zipEntryIndex))
                    throw new InvalidOperationException("The archive does not contain this entry.");

                return archive.Entries[zipEntryIndex].Open();
            }
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

        public void OnStreamDisposing(string key)
        {
            lock (openFiles)
            {
                if (openFiles.TryGetValue(key, out W9ArchiveOpenFile? entry) &&
                    entry != null &&
                    entry.RemoveRef())
                {
                    openFiles.Remove(key);
                }
            }
        }

        private NotifyingMemoryStream OpenInternal(string name)
        {
            lock (openFiles)
            {
                if (openFiles.TryGetValue(name, out W9ArchiveOpenFile? wof) && wof is not null)
                    return new NotifyingMemoryStream(name, wof.AddRef(), this);

                if (!archiveIndex.TryGetValue(name, out int zipEntryIndex))
                    throw new InvalidOperationException("The archive does not contain this entry.");

                long length = archive.Entries[zipEntryIndex].Length;
                using Stream s = archive.Entries[zipEntryIndex].Open();
                byte[] uncompressed = new byte[length];
                s.ReadExactly(uncompressed, 0, (int)length);

                W9ArchiveOpenFile wofnew = new W9ArchiveOpenFile(uncompressed);
                openFiles.Add(name,wofnew);

                return new NotifyingMemoryStream(name, wofnew.Data, this);
            }
        }
    }
}
