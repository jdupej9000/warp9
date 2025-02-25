using System.Collections.Generic;

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
    }
}
