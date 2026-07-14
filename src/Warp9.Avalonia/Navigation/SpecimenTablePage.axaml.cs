using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using System;
using System.Security.Policy;
using Warp9.Avalonia.Navigation;
using Warp9.Model;

namespace Warp9.Avalonia;

public record SpecimenTableColumnInfo(string name, string type)
{
    public string Name { get; init; } = name;
    public string Type { get; init; } = type;
    public override string ToString()
    {
        return $"{Name} ({Type})";
    }
}

public partial class SpecimenTablePage : ContentPage, IWarp9View
{
    public SpecimenTablePage()
    {
        InitializeComponent();
    }

    Warp9ProjectModel? model;
    long entryIndex = -1;
    bool fullResolveTable = false;

    SpecimenTable Table
    {
        get
        {
            if (entryIndex < 0 ||
                model is null ||
                !model.Project.Entries.TryGetValue(entryIndex, out ProjectEntry? entry))
                throw new InvalidOperationException();

            if (entry.Payload.Table is null)
                throw new InvalidOperationException();

            if (fullResolveTable)
                return ModelUtils.MakeFullSpecimenTable(model.Project, entryIndex) ?? throw new InvalidOperationException();

            return entry.Payload.Table;
        }
    }

    public void AttachViewModel(Warp9ProjectModel vm)
    {
        model = vm;
    }

    public void DetachViewModel()
    {
        model = null;
    }

    public void ShowEntry(long idx, bool fullResolve = false)
    {
        dataMain.Columns.Clear();

        entryIndex = idx;
        fullResolveTable = fullResolve;
        SpecimenTable table = Table;
        dataMain.ItemsSource = table;
        
        DataGridTextColumn colId = new DataGridTextColumn
        {
            Header = new SpecimenTableColumnInfo("ID", ""),
            Binding = new Binding("[!index]"),
            CanUserReorder = false,
            IsReadOnly = true
        };
        dataMain.Columns.Add(colId);

        foreach (var kvp in table.Columns)
        {
            string bindingKey = "[" + kvp.Key.Replace(' ', '$') + "]";

            switch (kvp.Value.ColumnType)
            {
                case SpecimenTableColumnType.Integer:
                case SpecimenTableColumnType.Real:
                case SpecimenTableColumnType.String:
                    {
                        DataGridTextColumn col = new DataGridTextColumn
                        {
                            Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                            Binding = new Binding(bindingKey)
                        };
                        dataMain.Columns.Add(col);
                    }
                    break;

                case SpecimenTableColumnType.Factor:
                    {
                        DataGridTextColumn col = new DataGridTextColumn
                        {
                            Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                            Binding = new Binding(bindingKey)
                        };
                        dataMain.Columns.Add(col);
                    }
                    break;

                case SpecimenTableColumnType.Boolean:
                    {
                        DataGridCheckBoxColumn col = new DataGridCheckBoxColumn
                        {
                            Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                            Binding = new Binding(bindingKey)
                        };
                        dataMain.Columns.Add(col);
                    }
                    break;
                case SpecimenTableColumnType.Image:
                case SpecimenTableColumnType.Mesh:
                case SpecimenTableColumnType.PointCloud:
                case SpecimenTableColumnType.Matrix:
                    {
                        DataGridTextColumn col = new DataGridTextColumn
                        {
                            Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                            Binding = new Binding(bindingKey),
                            IsReadOnly = true
                        };
                        dataMain.Columns.Add(col);
                    }
                    break;
               // default:
                //    throw new NotSupportedException();
            }
        }
    }
}