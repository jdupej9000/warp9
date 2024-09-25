﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Data;

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

        public abstract IEnumerable<T> GetData<T>();
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

        public override IEnumerable<TReq> GetData<TReq>()
        {
            if (data is List<TReq> typedData)
                return typedData;

            throw new InvalidOperationException();
        }
    }
}