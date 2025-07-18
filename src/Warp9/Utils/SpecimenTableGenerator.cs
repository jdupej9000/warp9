using System.Globalization;
using System.IO;
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
        public static SpecimenTable FromImporter(IUntypedTableProvider tableProvider, IEnumerable<SpecimenTableColumnImportOperation> ops, Project? project = null)
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

                    case ColumnImportType.Factor:
                        AddFactorColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name, op.Levels ?? Array.Empty<string>());
                        break;

                    case ColumnImportType.Boolean:
                        AddBoolColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name);
                        break;

                    case ColumnImportType.Image:
                        ArgumentNullException.ThrowIfNull(project);
                        AddReferenceColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name, tableProvider.WorkingDirectory, SpecimenTableColumnType.Image, project);
                        break;

                    case ColumnImportType.Mesh:
                        ArgumentNullException.ThrowIfNull(project);
                        AddReferenceColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name, tableProvider.WorkingDirectory, SpecimenTableColumnType.Mesh, project);
                        break;

                    case ColumnImportType.Landmarks:
                        ArgumentNullException.ThrowIfNull(project);
                        AddReferenceColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name, tableProvider.WorkingDirectory, SpecimenTableColumnType.PointCloud, project);
                        break;

                    case ColumnImportType.Matrix:
                        ArgumentNullException.ThrowIfNull(project);
                        AddReferenceColumn(table, tableProvider, op.SourceColumnIndices[0], op.Name, tableProvider.WorkingDirectory, SpecimenTableColumnType.Matrix, project);
                        break;

                    case ColumnImportType.Landmarks2DAos:
                    case ColumnImportType.Landmarks2DSoa: 
                    case ColumnImportType.Landmarks3DAos:
                    case ColumnImportType.Landmarks3DSoa:
                        throw new NotImplementedException();
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

        private static string[] FindFactorLevels(IUntypedTableProvider src, int idx)
        {
            HashSet<string> levels = new HashSet<string>();
            foreach (string[] row in src.ParsedData)
                levels.Add(row[idx]);

            return levels.ToArray();
        }

        private static void AddFactorColumn(SpecimenTable tab, IUntypedTableProvider src, int idx, string name, string[] levels)
        {
            string[] lvl = levels.Length == 0 ? FindFactorLevels(src, idx) : levels;

            Dictionary<string, int> search = new Dictionary<string, int>();
            for (int i = 0; i < levels.Length; i++)
                search.Add(levels[i], i);

            SpecimenTableColumn<int> col = tab.AddColumn<int>(name, SpecimenTableColumnType.Factor, levels);
            foreach (string[] row in src.ParsedData)
            {
                if (row.Length > idx)
                {
                    if (search.TryGetValue(row[idx], out int factorValue))
                        col.Add(factorValue);
                    else
                        col.Add();
                }
            }
        }

        private static void AddBoolColumn(SpecimenTable tab, IUntypedTableProvider src, int idx, string name)
        {
            SpecimenTableColumn<bool> col = tab.AddColumn<bool>(name, SpecimenTableColumnType.Boolean);
            foreach (string[] row in src.ParsedData)
            {
                if (row.Length > idx && bool.TryParse(row[idx], out bool x))
                    col.Add(x);
                else
                    col.Add();
            }
        }

        private static void AddReferenceColumn(SpecimenTable tab, IUntypedTableProvider src, int idx, string name, string wdir, SpecimenTableColumnType colType, Project proj)
        {
            SpecimenTableColumn<ProjectReferenceLink> col = tab.AddColumn<ProjectReferenceLink>(name, colType);
            foreach (string[] row in src.ParsedData)
            {
                if (row.Length <= idx)
                {
                    col.Add();
                    continue;
                }

                string referencePath = row[idx].Replace("//", "\\").Replace("\\\\", "\\");
                if(!Path.IsPathRooted(referencePath))
                    referencePath = Path.Combine(wdir, referencePath);

                ProjectReferenceFormat fmt = Path.GetExtension(referencePath).ToLower() switch
                {
                    ".obj" => ProjectReferenceFormat.ObjMesh,
                    ".ply" => ProjectReferenceFormat.PlyMesh,
                    ".txt" => ProjectReferenceFormat.MorphoLandmarks,
                    ".jpg" or ".jpe" or ".jpeg" or ".jfif" => ProjectReferenceFormat.JpegImage,
                    ".png" => ProjectReferenceFormat.PngImage,
                    _ => ProjectReferenceFormat.Invalid
                };

                if (fmt == ProjectReferenceFormat.Invalid)
                {
                    col.Add();
                    continue;
                }

                long referenceIndex = proj.AddReferenceExternal(referencePath, fmt);
                col.Add(new ProjectReferenceLink(referenceIndex));
            }
        }
    }
}