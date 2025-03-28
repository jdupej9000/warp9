﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Warp9.Model
{
    public enum SpecimenTableColumnType
    {
        Integer = 0,
        Real = 1,
        String = 2,
        Factor = 3,
        Boolean = 4,

        Image = 10,

        Mesh = 20,
        PointCloud = 21,
        Matrix = 22,

        Invalid = -1
    }

    public abstract class SpecimenTableColumn
    {
        [JsonPropertyName("type")]
        public SpecimenTableColumnType ColumnType { get; internal set; } = SpecimenTableColumnType.Invalid;

        [JsonIgnore]
        public abstract int NumRows { get; }

        [JsonIgnore]
        public string[]? Names { get; set; } = null;

        public abstract IReadOnlyList<T> GetData<T>();
        public abstract void Clear();

        public abstract object? GetAt(int idx);
        public abstract void SetAt(int idx, object? value);
        public abstract bool RemoveAt(int idx);
        public abstract void Add();
    }

    public class SpecimenTableColumn<T> : SpecimenTableColumn
    {
        public SpecimenTableColumn(SpecimenTableColumnType colType, string[]? names=null)
        {
            ColumnType = colType;
            Names = names;
        }

        public SpecimenTableColumn(SpecimenTableColumnType colType, List<T> d, string[]? names = null)
        {
            ColumnType = colType;
            Names = names;
            data = d;
        }

        List<T> data = new List<T>();

        public override int NumRows => data.Count;
        public List<T> Data => data;

        public override IReadOnlyList<TReq> GetData<TReq>()
        {
            if (data is List<TReq> typedData)
                return typedData;

            throw new InvalidOperationException();
        }

        public override void Clear()
        {
            data.Clear();
        }

        public override object? GetAt(int idx)
        {
            return data[idx];
        }

        public override void SetAt(int idx, object? value)
        {
            if(value is T typedValue)
                data[idx] = typedValue;
        }

        public override bool RemoveAt(int idx)
        {
            data.RemoveAt(idx);
            return true;
        }

        public void Add(T v)
        {
            data.Add(v); 
        }

        public override void Add()
        {
            if (data is List<string> stringData)
                stringData.Add(string.Empty);
            else
                data.Add(default!);
        }
    }
}
