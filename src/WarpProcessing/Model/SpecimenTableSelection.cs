using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Warp9.Native;

namespace Warp9.Model
{
    public class SpecimenTableSelectionRow
    {
        public SpecimenTableSelectionRow(SpecimenTableSelection sts, int index, SpecimenTableRow parentRow)
        {
            Index = index;
            Parent = sts;
            ParentRow = parentRow;
        }

        public int Index { get; init; }
        public bool IsSelected
        {
            get { return Parent.Selected[Index]; }
            set { Parent.Selected[Index] = value; }
        }

        public SpecimenTableSelection Parent { get; init; }
        public SpecimenTableRow ParentRow { get; init; }

        public object? this[string colKey]
        {
            get => ParentRow.GetSafeTypedValue(colKey);
        }
    }

    public class SpecimenTableSelectionEnumerator : IEnumerator, IEnumerator<SpecimenTableSelectionRow>
    {
        public SpecimenTableSelectionEnumerator(SpecimenTableSelection sts)
        {
            parent = sts;
            index = 0;
            numRows = sts.Count;
        }

        private readonly SpecimenTableSelection parent;
        private int index, numRows;

        public object Current => parent.MakeRow(index);
        SpecimenTableSelectionRow IEnumerator<SpecimenTableSelectionRow>.Current => parent.MakeRow(index);

        public bool MoveNext()
        {
            if (index >= numRows)
                return false;

            index++;
            return true;
        }

        public void Reset()
        {
            index = 0;
        }

        public void Dispose()
        {
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class SpecimenTableSelection :
        IList<SpecimenTableSelectionRow>,
        IList,
        INotifyCollectionChanged,
        INotifyPropertyChanged,
        IQueryable
    {
        public SpecimenTableSelection(SpecimenTable tab)
        {
            specTable = tab;
            selected = new bool[tab.Count];
        }

        private readonly SpecimenTable specTable;
        bool[] selected;

        public int Count => selected.Length;
        public bool IsReadOnly => false;
        public bool IsFixedSize => true;
        public bool IsSynchronized => false;
        public object SyncRoot => throw new NotImplementedException();
        public bool[] Selected => selected;
        public IReadOnlyDictionary<string, SpecimenTableColumn> TableColumns => specTable.Columns;

        public Type ElementType => typeof(SpecimenTableSelectionRow);

        public Expression Expression => throw new NotImplementedException();

        public IQueryProvider Provider => throw new NotImplementedException();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        object? IList.this[int index]
        {
            get => MakeRow(index);
            set => throw new NotImplementedException();
        }

        public SpecimenTableSelectionRow this[int index]
        {
            get => MakeRow(index);
            set => throw new NotImplementedException();
        }

        public SpecimenTableSelectionRow MakeRow(int index)
        {
            return new SpecimenTableSelectionRow(this, index, specTable.MakeRow(index));
        }

        public int IndexOf(SpecimenTableSelectionRow item)
        {
            if (item.Parent != this)
                return -1;

            return item.Index;
        }

        public void Insert(int index, SpecimenTableSelectionRow item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(SpecimenTableSelectionRow item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(SpecimenTableSelectionRow item)
        {
            return item.Parent == this;
        }

        public void CopyTo(SpecimenTableSelectionRow[] array, int arrayIndex)
        {
            int numRows = Count;
            for (int i = 0; i < numRows; i++)
                array[i + arrayIndex] = MakeRow(i);
        }

        public bool Remove(SpecimenTableSelectionRow item)
        {
            return false;
        }

        public IEnumerator<SpecimenTableSelectionRow> GetEnumerator()
        {
            return new SpecimenTableSelectionEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SpecimenTableSelectionEnumerator(this);
        }

        public int Add(object? value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object? value)
        {
            if (value is not SpecimenTableSelectionRow stsr)
                return false;

            return stsr.Parent == this;
        }

        public int IndexOf(object? value)
        {
            if (value is not SpecimenTableSelectionRow stsr)
                return -1;

            return stsr.Index;
        }

        public void Insert(int index, object? value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object? value)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }
    }
}
