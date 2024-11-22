﻿using System.Collections.Generic;

namespace Warp9.Model
{
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
                entry.Kind != ProjectEntryKind.Specimens ||
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
    }
}
