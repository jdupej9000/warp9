using System;
using System.Buffers;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Processing;
using Warp9.Utils;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace Warp9.Model
{
    public record SpecimenTableColumnInfo (long SpecTableId, string SpecTableName, string ColumnName, SpecimenTableColumn Column)
    {
        public override string ToString()
        {
            return ColumnName;
        }
    }

    public record SpecimenTableInfo(long SpecTableId, string SpecTableName, SpecimenTable SpecimenTable)
    {
        public override string ToString()
        {
            return string.Format("{0} ({1} rows)",
                SpecTableName, SpecimenTable.Count);
        }
    }

    public static class ModelUtils
    {
        public static ReferencedData<T> Resolve<T>(Project proj, ReferencedData<T> x) where T : class
        {
            if(x.IsLoaded)
                return x;

            if (proj.TryGetReference(x.Key, out T? val) && val is not null)
                return new ReferencedData<T>(val, x.Key);

            return x;
        }

        public static ReferencedData<BufferSegment<T>> ResolveAsMeshView<T>(Project proj, MeshSegmentSemantic semantic, ReferencedData<BufferSegment<T>> x) where T : struct
        {
            if (x.IsLoaded)
                return x;

            PointCloud pcl;

            if (proj.TryGetReference(x.Key, out Mesh? valm) && valm is not null)
                pcl = valm;
            else if (proj.TryGetReference(x.Key, out PointCloud? valp) && valp is not null)
                pcl = valp;
            else
                return x;

            if(!pcl.TryGetData(semantic, out BufferSegment<T> buffer))
                return x;

            return new ReferencedData<BufferSegment<T>>(buffer, x.Key);
        }

        public static SpecimenTable? TryGetSpecimenTable(Project proj, long tableKey)
        {
            if (!proj.Entries.TryGetValue(tableKey, out ProjectEntry? entry) ||
                entry.Kind != ProjectEntryKind.Specimens ||
                entry.Payload.Table is null)
            {
                return null;
            }

            return entry.Payload.Table;
        }

        public static SpecimenTableColumn<T>? TryGetSpecimenTableColumn<T>(Project proj, long tableKey, string column)
        {
            if (!proj.Entries.TryGetValue(tableKey, out ProjectEntry? entry) ||                
                entry.Payload.Table is null ||
                !entry.Payload.Table.Columns.TryGetValue(column, out SpecimenTableColumn? col) ||
                col is not SpecimenTableColumn<T> typedCol)
            {
                return null;
            }

            return typedCol;
        }

        public static IEnumerable<T?> LoadSpecimenTableRefs<T>(Project proj, SpecimenTableColumn<ProjectReferenceLink> col)
            where T : class
        {
            foreach (ProjectReferenceLink link in col.GetData<ProjectReferenceLink>())
            {
                if (proj.TryGetReference(link.ReferenceIndex, out T? val))
                    yield return val;
                else
                    yield return null;
            }
        }

        public static T? LoadSpecimenTableRef<T>(Project proj, SpecimenTableColumn<ProjectReferenceLink> col, int index)
            where T : class
        {
            IReadOnlyList<ProjectReferenceLink> links = col.GetData<ProjectReferenceLink>();

            if (index >= links.Count)
                return null;

            if(proj.TryGetReference(links[index].ReferenceIndex, out T? val))
                return val;

            return null;
        }

        public static IEnumerable<SpecimenTableColumnInfo> EnumerateAllTableColumns(Project proj)
        {
            foreach (var kvp in proj.Entries)
            {
                if (kvp.Value.Payload.Table is SpecimenTable table)
                {
                    foreach (var col in table.Columns)
                        yield return new SpecimenTableColumnInfo(kvp.Key, kvp.Value.Name, col.Key, col.Value);
                }
            }
        }

        public static IEnumerable<SpecimenTableInfo> EnumerateSpecimenTables(Project proj)
        {
            foreach (var kvp in proj.Entries)
            {
                if (kvp.Value.Kind == ProjectEntryKind.Specimens &&
                    kvp.Value.Payload.Table is SpecimenTable table)
                {
                    yield return new SpecimenTableInfo(kvp.Key, kvp.Value.Name, table);
                }
            }
        }

        public static IEnumerable<SpecimenTableInfo> EnumerateEntitiesWithTables(Project proj)
        {
            foreach (var kvp in proj.Entries)
            {
                if (kvp.Value.Payload.Table is SpecimenTable table)
                {
                    yield return new SpecimenTableInfo(kvp.Key, kvp.Value.Name, table);
                }
            }
        }

        public static List<PointCloud> LoadModelsAsPclsWithSize(Project proj, long entityKey, string columnName, string? sizeColumn)
        {
            SpecimenTableColumn<ProjectReferenceLink>? corrColumn = TryGetSpecimenTableColumn<ProjectReferenceLink>(
                proj, entityKey, columnName);

            if (corrColumn is null)
                throw new ModelException($"Entity #{entityKey} does not contain a mesh/point cloud column '{columnName}'.");

            List<PointCloud?> dcaCorrPcls = LoadSpecimenTableRefs<PointCloud>(proj, corrColumn).ToList();
            if (dcaCorrPcls.Exists((t) => t is null))
                throw new ModelException("Selected specimen table column contains incomplete data.");

            int nv = dcaCorrPcls[0]!.VertexCount;
            int ns = dcaCorrPcls.Count;

            if (sizeColumn is not null)
            {
                SpecimenTableColumn<double>? csColumn = ModelUtils.TryGetSpecimenTableColumn<double>(
                    proj, entityKey, sizeColumn);

                if (csColumn is null)
                    throw new ModelException($"Entity #{entityKey} does contain a numeric column '{sizeColumn}'.");

                IReadOnlyList<double> cs = csColumn.GetData<double>();

                for (int i = 0; i < ns; i++)
                    dcaCorrPcls[i] = MeshScaling.ScalePosition(dcaCorrPcls[i]!, (float)cs[i]).ToPointCloud();
            }

            return dcaCorrPcls!;
        }

        public static bool[] MakeAllowList(Project proj, int nv, bool useThreshold, float thresh, long rejectMatrixKey)
        {
            // thresh is between 0 and 1

            if (nv < 1)
                throw new ModelException("Invalid vertex count.");

            bool[] allow = new bool[nv];

            if (useThreshold)
            {
                if (!proj.TryGetReference(rejectMatrixKey, out MatrixCollection? rejmc) ||
                    rejmc is null ||
                    !rejmc.TryGetMatrix(ModelConstants.VertexRejectionRatesKey, out Matrix<float>? rejectRates) ||
                    rejectRates is null)
                {
                    throw new ModelException("Vertex rejection rates could be loaded.");
                }

                MiscUtils.ThresholdBelow(rejectRates.Data.AsSpan(), thresh, allow.AsSpan());
            }
            else
            {
                for (int i = 0; i < nv; i++)
                    allow[i] = true;
            }

            return allow;
        }

        public static string DescribeSpecimenSelection(SpecimenTable spec, bool[] sel, out bool isComplete)
        {
            int firstSel = Array.IndexOf(sel, true);
            if (firstSel == -1)
            {
                isComplete = true;
                return "nothing";
            }

            int n = sel.Length;
            bool[] selSynth = ArrayPool<bool>.Shared.Rent(n);
            Array.Fill(selSynth, true);

            List<string> conditions = new List<string>();

            foreach (var kvp in spec.Columns)
            {
                SpecimenTableColumn col = kvp.Value;
                if (kvp.Value.ColumnType == SpecimenTableColumnType.Integer && 
                    col is SpecimenTableColumn<long> colint)
                {
                    bool isOneValue = !colint.Data
                        .Index()
                        .Where((t) => sel[t.Index])
                        .Any((t) => t.Item != colint.Data[firstSel]);

                    if (isOneValue)
                    {
                        conditions.Add($"{kvp.Key}={colint.Data[firstSel]}");
                        for (int i = 0; i < n; i++)
                        {
                            if (colint.Data[i] != colint.Data[firstSel])
                                selSynth[i] = false;
                        }
                    }
                }
                else if (kvp.Value.ColumnType == SpecimenTableColumnType.Factor &&
                   col is SpecimenTableColumn<int> colfact)
                {
                    bool isOneValue = !colfact.Data
                        .Index()
                        .Where((t) => sel[t.Index])
                        .Any((t) => t.Item != colfact.Data[firstSel]);

                    if (isOneValue)
                    {
                        conditions.Add($"{kvp.Key}={col.Names![colfact.Data[firstSel]]}");
                        for (int i = 0; i < n; i++)
                        {
                            if (colfact.Data[i] != colfact.Data[firstSel])
                                selSynth[i] = false;
                        }
                    }
                }
                else if (kvp.Value.ColumnType == SpecimenTableColumnType.String &&
                   col is SpecimenTableColumn<string> colstr)
                {
                    bool isOneValue = !colstr.Data
                        .Index()
                        .Where((t) => sel[t.Index])
                        .Any((t) => t.Item != colstr.Data[firstSel]);

                    if (isOneValue)
                    {
                        conditions.Add($"{kvp.Key}={colstr.Data[firstSel]}");
                        for (int i = 0; i < n; i++)
                        {
                            if (colstr.Data[i] != colstr.Data[firstSel])
                                selSynth[i] = false;
                        }
                    }
                }
                else if (kvp.Value.ColumnType == SpecimenTableColumnType.Integer &&
                   col is SpecimenTableColumn<bool> colbool)
                {
                    bool isOneValue = !colbool.Data
                        .Index()
                        .Where((t) => sel[t.Index])
                        .Any((t) => t.Item != colbool.Data[firstSel]);

                    if (isOneValue)
                    {
                        conditions.Add($"{kvp.Key}={colbool.Data[firstSel]}");
                        for (int i = 0; i < n; i++)
                        {
                            if (colbool.Data[i] != colbool.Data[firstSel])
                                selSynth[i] = false;
                        }
                    }
                }
            }

            isComplete = true;
            for (int i = 0; i < n; i++)
            {
                if (sel[i] != selSynth[i])
                {
                    isComplete = false;
                    break;
                }
            }

            ArrayPool<bool>.Shared.Return(selSynth);

            if (conditions.Count == 0)
            {
                int numSel = sel.Count((t) => t == true);
                isComplete = false;
                return string.Format("{0} points", numSel);
            }
            
            return string.Join(" and ", conditions);
        }
    }
}
