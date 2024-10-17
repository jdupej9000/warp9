﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public static class ModelUtils
    {
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
    }
}