using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Warp9.Data;
using Warp9.IO;

namespace Warp9.Model
{
    public class Project : IDisposable
    {
        private Project()
        {
            archive = null;
            InitJsonOptions();
        }

        internal Project(IProjectArchive archive)
        {
            this.archive = archive;
            InitJsonOptions();
        }

        IProjectArchive? archive;
        readonly Dictionary<long, ProjectReference> references = new Dictionary<long, ProjectReference>();
        Dictionary<long, ProjectEntry> entries = new Dictionary<long, ProjectEntry>();
        ProjectSettings settings = new ProjectSettings();
        UniqueIdGenerator objectIdGen = new UniqueIdGenerator();
        UniqueIdGenerator specimenIdGen = new UniqueIdGenerator();


        public static readonly string ManifestFileName = "manifest.json";
        private static readonly string ObjectIdGenName = "objects";
        private static readonly string SpecimenIdGenName = "specimens";
        private static JsonSerializerOptions? opts;

        public bool IsArchiveOpen => archive?.IsOpen ?? false;
        public ProjectSettings Settings => settings;
        public IReadOnlyDictionary<long, ProjectEntry> Entries => entries;


        public bool TryGetReference<T>(long index, [MaybeNullWhen(false)] out T value)
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
        public long AddReferenceDirect<T>(ProjectReferenceFormat fmt, T val)
        {
            long index = objectIdGen.Next();

            string fileName = fmt switch
            {
                ProjectReferenceFormat.W9Mesh => string.Format("ref-{0:x}.w9mesh", index),
                ProjectReferenceFormat.W9Pcl => string.Format("ref-{0:x}.w9pcl", index),
                ProjectReferenceFormat.W9Matrix => string.Format("ref-{0:x}.w9mat", index),
                _ => throw new ArgumentException(nameof(fmt))
            };

            ProjectReference reference = new ProjectReference(index,
                 new ProjectReferenceInfo() { FileName = fileName, Format = fmt, IsInternal = true }, val);
            references.Add(index, reference);
            return index;
        }

        public long AddReferenceDirect<T>(string fileName, ProjectReferenceFormat fmt, T val)
        {
            long index = objectIdGen.Next();
            ProjectReference reference = new ProjectReference(index,
                 new ProjectReferenceInfo() { FileName = fileName, Format = fmt, IsInternal = true }, val);
            references.Add(index, reference);
            return index;
        }

        public long AddReferenceExternal(string fileName, ProjectReferenceFormat fmt)
        {
            string fileNameMinimal = fileName;

            if (archive is not null)
                Path.GetRelativePath(archive.WorkingDirectory, fileName);

            long index = objectIdGen.Next();
            ProjectReference reference = new ProjectReference(index,
                new ProjectReferenceInfo() { FileName = fileNameMinimal, Format = fmt, IsInternal = false });
           
            references.Add(index, reference);
            return index;
        }

        public bool RemoveReference(long index)
        {
            return references.Remove(index);
        }

        public ProjectEntry AddNewEntry(ProjectEntryKind kind)
        {
            long index = objectIdGen.Next();
            ProjectEntry entry = new ProjectEntry(index, kind);
            entries[index] = entry;
            return entry;
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

            if (manifest.Counters.TryGetValue(ObjectIdGenName, out UniqueIdGenerator? igobj))
                objectIdGen = igobj;
            else
                objectIdGen = new UniqueIdGenerator();

            if (manifest.Counters.TryGetValue(SpecimenIdGenName, out UniqueIdGenerator? igspec))
                specimenIdGen = igspec;
            else
                specimenIdGen = new UniqueIdGenerator();
        }

        public void MakeManifest(Stream s, Dictionary<long, ProjectReferenceInfo> refs)
        {
            ProjectManifest manifest = new ProjectManifest();
            manifest.Settings = settings;
            manifest.References = refs;
            manifest.Entries = entries;
            manifest.Counters[ObjectIdGenName] = objectIdGen;
            manifest.Counters[SpecimenIdGenName] = specimenIdGen;

            JsonSerializer.Serialize(s, manifest, opts);
        }

        public void Dispose()
        {
            archive?.Dispose();
        }

        public IProjectArchive? SwitchToSavedArchive(IProjectArchive newArchive)
        {
            IProjectArchive? oldArchive = archive;
            archive = newArchive;

            // Forget all native objects in the references, as they are now contained in 
            // the new archive.
            foreach (var kvp in references)
                kvp.Value.NativeObject = null;

            return oldArchive;
        }

        private ProjectReferenceInfo MakeReferenceInternal(IProjectArchive destArchive, long key, ProjectReferenceInfo ext)
        {
            string workingDir = destArchive.WorkingDirectory;
            string sourcePath = ext.WithAbsolutePath(workingDir).FileName;
            using FileStream sourceFile = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);

            ProjectReferenceInfo ret;
            switch (ext.Format)
            {
                case ProjectReferenceFormat.ObjMesh:
                    {
                        string internalRefName = string.Format("ref-{0:x}.w9mesh", key);
                        using Stream destStream = destArchive.CreateFile(internalRefName);

                        if (!ObjImport.TryImport(sourceFile, ObjImportMode.PositionsOnly, out Mesh objMesh, out _))
                            throw new InvalidDataException("Filed to load " + sourcePath);

                        WarpBinExport.ExportMesh(destStream, objMesh);
                        ret = ProjectReferenceInfo.CreateInternal(internalRefName, ProjectReferenceFormat.W9Mesh);
                    }
                    break;

                case ProjectReferenceFormat.MorphoLandmarks:
                    {
                        string internalRefName = string.Format("ref-{0:x}.w9pcl", key);
                        using Stream destStream = destArchive.CreateFile(internalRefName);

                        if (!MorphoLandmarkImport.TryImport(sourceFile, out PointCloud lms, out _))
                            throw new InvalidDataException("Filed to load " + sourcePath);

                        WarpBinExport.ExportPcl(destStream, lms);
                        ret = ProjectReferenceInfo.CreateInternal(internalRefName, ProjectReferenceFormat.W9Pcl);
                    }
                    break;

                case ProjectReferenceFormat.W9Mesh:
                case ProjectReferenceFormat.W9Pcl:
                case ProjectReferenceFormat.W9Matrix:
                case ProjectReferenceFormat.PngImage:
                case ProjectReferenceFormat.JpegImage:
                    {
                        string extension = ext.Format switch
                        {
                            ProjectReferenceFormat.W9Mesh => "w9mesh",
                            ProjectReferenceFormat.W9Pcl => "w9pcl",
                            ProjectReferenceFormat.W9Matrix => "w9mx",
                            ProjectReferenceFormat.PngImage => "png",
                            ProjectReferenceFormat.JpegImage => "jpg",
                            _ => throw new NotImplementedException()
                        };

                        string internalRefName = string.Format("ref-{0:x}.{1}", key, extension);
                        using Stream destStream = destArchive.CreateFile(internalRefName);
                        sourceFile.CopyTo(destStream);

                        ret = ProjectReferenceInfo.CreateInternal(internalRefName, ext.Format);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            return ret;
        }

        public void Save(IProjectArchive destArchive, IProgressProvider? progress=null)
        {
            progress?.StartBatch(1 + references.Count);

            Dictionary<long, ProjectReferenceInfo> savedRefs = new Dictionary<long, ProjectReferenceInfo>();
            string workingDir = destArchive.WorkingDirectory;

            int refIdx = 0;
            foreach(var kvp in references)
            {
                progress?.StartTask(refIdx);
                long refKey = kvp.Key;
                ProjectReference pr = kvp.Value;

                if (pr.Info.IsInternal && !pr.HasNativeObject)
                {
                    if (archive is null)
                        throw new InvalidOperationException("Cannot copy reference from a closed archive.");

                    destArchive.CopyFileFrom(pr.Info.FileName, archive);
                    savedRefs[refKey] = pr.Info;
                }
                else if (pr.Info.IsInternal && pr.HasNativeObject)
                {
                    using Stream destRefStream = destArchive.CreateFile(pr.Info.FileName);
                    if (!CodecBank.ProjectCodecs.TryEncodeObject(destRefStream, pr.NativeObject!, pr.Info.Format, null))
                        throw new NotSupportedException();

                    savedRefs[refKey] = pr.Info;
                }
                else if(!pr.Info.IsInternal) // external reference
                {
                    savedRefs[refKey] = settings.ExternalReferencePolicy switch
                    {
                        ProjectExternalReferencePolicy.KeepExternalAbsolutePaths => pr.Info.WithAbsolutePath(workingDir),
                        ProjectExternalReferencePolicy.KeepExternalRelativePaths => pr.Info.WithRelativePath(workingDir),
                        ProjectExternalReferencePolicy.ConvertToInternal => MakeReferenceInternal(destArchive, refKey, pr.Info),
                        _ => throw new NotImplementedException("This external reference policy is not implemented.")
                    };
                }

                progress?.FinishTask(refIdx);
                refIdx++;
            }

            int manifestTask = references.Count;
            progress?.StartTask(manifestTask);
            using (Stream streamManifest = destArchive.CreateFile(ManifestFileName))
            {
                MakeManifest(streamManifest, savedRefs);
            }
            progress?.FinishTask(manifestTask);

            progress?.EndBatch();
        }

        private void InitJsonOptions()
        {
            opts = new JsonSerializerOptions()
            {
                AllowTrailingCommas = false,
                //WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            opts.Converters.Add(new SpecimenTableJsonConverter());
        }

        public static Project CreateEmpty()
        {
            return new Project();
        }

        public static Project Load(IProjectArchive archive)
        {
            Project ret = new Project(archive);
            ret.LoadManifest();
            return ret;
        }
    }
}
