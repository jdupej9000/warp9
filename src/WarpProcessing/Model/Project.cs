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
            archive = null;
        }

        internal Project(IProjectArchive archive)
        {
            this.archive = archive;
        }

        IProjectArchive? archive;
        readonly Dictionary<int, ProjectReference> references = new Dictionary<int, ProjectReference>();
        Dictionary<int, ProjectEntry> entries = new Dictionary<int, ProjectEntry>();
        ProjectSettings settings = new ProjectSettings();
        
        private static readonly string ManifestFileName = "manifest.json";
        private static readonly JsonSerializerOptions opts = new JsonSerializerOptions()
        {
            AllowTrailingCommas = false,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public bool IsArchiveOpen => archive?.IsOpen ?? false;
        public ProjectSettings Settings => settings;


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
                if (archive is null ||
                    !archive.ContainsFile(reference.Info.FileName))
                {
                    value = default;
                    return false;
                }

                using Stream zipEntryStream = archive.OpenFile(reference.Info.FileName);
                return CodecBank.ProjectCodecs.TryDecode<T>(zipEntryStream, reference.Info.Format, null, out value);
            }

            string externalPath = reference.Info.FileName;
            if (!Path.IsPathRooted(externalPath))
            {
                if (archive is null)
                    throw new InvalidOperationException();

                externalPath = Path.Combine(archive.WorkingDirectory, externalPath);
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

        public int AddReferenceDirect<T>(string fileName, ProjectReferenceFormat fmt, T val)
        {
            int index = 0; // TODO
            ProjectReference reference = new ProjectReference(index,
                 new ProjectReferenceInfo() { FileName = fileName, Format = fmt, IsInternal = true }, val);
            references.Add(index, reference);
            return index;
        }

        public int AddReferenceExternal(string fileName, ProjectReferenceFormat fmt)
        {
            string fileNameMinimal = fileName;

            if (archive is not null)
                Path.GetRelativePath(archive.WorkingDirectory, fileName);

            int index = 0; // TODO
            ProjectReference reference = new ProjectReference(index,
                new ProjectReferenceInfo() { FileName = fileNameMinimal, Format = fmt, IsInternal = false });
           
            references.Add(index, reference);
            return index;
        }

        public bool RemoveReference(int index)
        {
            return references.Remove(index);
        }

        private void LoadManifest()
        {
            if (archive is null || !archive.ContainsFile(ManifestFileName))
                throw new InvalidOperationException();

            using Stream manifestStream = archive.OpenFile(ManifestFileName);
            ParseRawManifest(manifestStream);
        }

        private void ParseRawManifest(Stream s)
        {
            ProjectManifest? manifest = JsonSerializer.Deserialize<ProjectManifest>(s, opts);
            if (manifest is null)
                throw new InvalidDataException();

            foreach (var kvp in manifest.References)
                references.Add(kvp.Key, new ProjectReference(kvp.Key, kvp.Value));

            entries = manifest.Entries;

            settings = manifest.Settings;
        }

        public void MakeManifest(Stream s)
        {
            ProjectManifest manifest = new ProjectManifest();
            manifest.Settings = settings;

            foreach (var kvp in references)
                manifest.References.Add(kvp.Key, kvp.Value.Info);

            manifest.Entries = entries;

            JsonSerializer.Serialize(s, manifest, opts);
        }

        public void Dispose()
        {
            archive?.Dispose();
        }

        public void Save(IProjectArchive destArchive, IProgressProvider? progress=null)
        {
            progress?.StartBatch(1 + references.Count);

            progress?.StartTask(0);
            using (Stream streamManifest = destArchive.CreateFile(ManifestFileName))
            {
                MakeManifest(streamManifest);
            }
            progress?.FinishTask(0);

            int refIdx = 0;
            foreach(var kvp in references)
            {
                progress?.StartTask(refIdx + 1);
                ProjectReference pr = kvp.Value;

                if (pr.Info.IsInternal && !pr.HasNativeObject)
                {
                    if (archive is null)
                        throw new InvalidOperationException("Cannot copy reference from a closed archive.");

                    destArchive.CopyFileFrom(pr.Info.FileName, archive);
                }
                else if (pr.Info.IsInternal && pr.HasNativeObject)
                {
                    using Stream destRefStream = destArchive.CreateFile(pr.Info.FileName);
                    if (!CodecBank.ProjectCodecs.TryEncodeObject(destRefStream, pr.NativeObject!, pr.Info.Format, null))
                        throw new NotSupportedException();
                }
                else
                {
                    // keep external references external
                }

                progress?.FinishTask(refIdx + 1);
                refIdx++;
            }

            progress?.EndBatch();
            // TODO: add save options with reference transcoding
        }

        public static Project CreateEmpty()
        {
            return new Project();
        }

       /* public static Project Load(string path, bool keepOpen=true)
        {
            ZipArchive zip = ZipFile.OpenRead(path);
            
            Project ret = new Project(path, zip);
            ret.LoadManifest();

            if (!keepOpen)
                ret.Close();

            ret.MakeArchiveIndex();

            return ret;
        }*/
    }
}
