using System;
using System.Globalization;
using Warp9.Data;
using Warp9.Model;

namespace Warp9.Utils
{
    public enum ColumnImportType
    {
        Integer = 0,
        Real,
        String,
        Factor,
        Boolean,
        Image,
        Mesh,
        Landmarks,
        Matrix,
        Landmarks2DAos,
        Landmarks2DSoa,
        Landmarks3DAos,
        Landmarks3DSoa
    }

    public record SpecimenTableColumnImportOperation(string name, ColumnImportType type, int[] indices, string[]? levels)
    {
        public string Name { get; init; } = name;
        public ColumnImportType ColumnImportType { get; init; } = type;
        public int[] SourceColumnIndices { get; init; } = indices;
        public string[]? Levels { get; init; } = levels;
    }

    public static class SpecimenTableGenerator
    {
        public static SpecimenTable FromImporter(IUntypedTableProvider tableProvider, IEnumerable<SpecimenTableColumnImportOperation> ops)
        {
            SpecimenTable table = new SpecimenTable();

            foreach (SpecimenTableColumnImportOperation op in ops)
            {
                switch (op.ColumnImportType)
                {
                    case ColumnImportType.Integer:
                        AddIntegerColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name);
                        break;

                    case ColumnImportType.Real:
                        AddRealColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name);
                        break;

                    case ColumnImportType.String:
                        AddStringColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name);
                        break;

                    case ColumnImportType.Factor: break;
                    case ColumnImportType.Boolean: break;
                    case ColumnImportType.Image: break;
                    case ColumnImportType.Mesh: break;
                    case ColumnImportType.Landmarks: break;
                    case ColumnImportType.Matrix: break;
                    case ColumnImportType.Landmarks2DAos: break;
                    case ColumnImportType.Landmarks2DSoa: break;
                    case ColumnImportType.Landmarks3DAos: break;
                    case ColumnImportType.Landmarks3DSoa: break;
                }
            }

            return table;
        }

        private static void AddIntegerColumn(SpecimenTable tab, IUntypedTableProvider src, int idx, string name)
        {
            SpecimenTableColumn<long> col = tab.AddColumn<long>(name, SpecimenTableColumnType.Integer);
            foreach (string[] row in src.ParsedData)
            {
                if (row.Length > idx && long.TryParse(row[idx], CultureInfo.InvariantCulture, out long x))
                    col.Add(x);
                else
                    col.Add();
            }
        }

        private static void AddRealColumn(SpecimenTable tab, IUntypedTableProvider src, int idx, string name)
        {
            SpecimenTableColumn<double> col = tab.AddColumn<double>(name, SpecimenTableColumnType.Real);
            foreach (string[] row in src.ParsedData)
            {
                if (row.Length > idx && double.TryParse(row[idx], CultureInfo.InvariantCulture, out double x))
                    col.Add(x);
                else
                    col.Add();
            }
        }

        private static void AddStringColumn(SpecimenTable tab, IUntypedTableProvider src, int idx, string name)
        {
            SpecimenTableColumn<string> col = tab.AddColumn<string>(name, SpecimenTableColumnType.String);
            foreach (string[] row in src.ParsedData)
            {
                if (row.Length > idx)
                    col.Add(row[idx]);
                else
                    col.Add();
            }
        }
    }
}