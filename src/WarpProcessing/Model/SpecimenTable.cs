﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class SpecimenTableRow
    {
        public SpecimenTableRow(SpecimenTable t, int i)
        {
            table = t;
            rowIndex = i;
        }

        SpecimenTable table;
        int rowIndex;

        public SpecimenTable ParentTable => table;
        public int RowIndex => rowIndex;

        public object? this[string column]
        {
            get
            {
                return table.Columns[column].GetAt(rowIndex);
            }
            set
            {
                table.Columns[column].SetAt(rowIndex, value);
            }
        }
    };

    public class SpecimenTable :
        IList<SpecimenTableRow>, 
        IList, 
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        [JsonPropertyName("cols")]
        public Dictionary<string, SpecimenTableColumn> Columns { get; set; } = new Dictionary<string, SpecimenTableColumn>();


        public SpecimenTableColumn<T> AddColumn<T>(string name, SpecimenTableColumnType type, string[]? names = null)
        {
            // TODO: validate against allowed types
            SpecimenTableColumn<T> col = new SpecimenTableColumn<T>(type, names);
            Columns.Add(name, col);
            return col;
        }

        public SpecimenTableRow MakeRow(int index)
        {
            return new SpecimenTableRow(this, index);
        }

        public IEnumerable<SpecimenTableRow> GetRows()
        {
            int numRows = Count;
            for (int i = 0; i < numRows; i++)
                yield return MakeRow(i);
        }


        public int Count => Columns.Values.First().NumRows;
        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => throw new NotImplementedException();

        object? IList.this[int index] 
        { 
            get => MakeRow(index); 
            set => throw new NotImplementedException(); 
        }

        public SpecimenTableRow this[int index] 
        { 
            get => MakeRow(index); 
            set => throw new NotImplementedException(); 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void Add(SpecimenTableRow item)
        {
            throw new NotImplementedException();

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Clear()
        {
            foreach (var kvp in Columns)
                kvp.Value.Clear();

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(SpecimenTableRow item)
        {
            return item.ParentTable == this && Count > item.RowIndex;
        }

        public void CopyTo(SpecimenTableRow[] array, int arrayIndex)
        {
            int numRows = Count;
            for (int i = 0; i < numRows; i++)
                array[i + arrayIndex] = MakeRow(i);
        }

        public IEnumerator<SpecimenTableRow> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(SpecimenTableRow item)
        {
            if (item.ParentTable != this)
                return false;

            bool res = true;
            foreach (var kvp in Columns)
                res &= kvp.Value.RemoveAt(item.RowIndex);

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            return res;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // TODO
            throw new NotImplementedException();
        }

        public int IndexOf(SpecimenTableRow item)
        {
            if (item.ParentTable != this)
                throw new InvalidOperationException();

            return item.RowIndex;
        }

        public void Insert(int index, SpecimenTableRow item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            if (index >= Count) return;

            foreach (var kvp in Columns)
                kvp.Value.RemoveAt(index);

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public int Add(object? value)
        {
            if (value is not SpecimenTableRow row)
                throw new InvalidOperationException();

            throw new NotImplementedException();
        }

        public bool Contains(object? value)
        {
            if (value is not SpecimenTableRow row)
                return false;

            if (row.ParentTable != this)
                return false;

            return Count > row.RowIndex; // TODO: compare elements
        }

        public int IndexOf(object? value)
        {
            if (value is not SpecimenTableRow row)
                return -1;

            if (row.ParentTable != this || Count <= row.RowIndex)
                return -1;

            return row.RowIndex;
        }

        public void Insert(int index, object? value)
        {
            throw new NotImplementedException();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Remove(object? value)
        {
            if (value is not SpecimenTableRow row ||
                row.ParentTable != this)
                return;

            RemoveAt(row.RowIndex);
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }
    }
}
