using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class SpecimenTableColumnJsonConverter : JsonConverter<SpecimenTableColumn>
    {
        public override SpecimenTableColumn? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            if (reader.GetString() != "type")
                throw new JsonException();

            reader.Read();
            if(reader.TokenType != JsonTokenType.Number)
                throw new JsonException();

            SpecimenTableColumnType colType = (SpecimenTableColumnType)reader.GetInt32();
            List<string>? colNames = null;

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            if (reader.GetString() == "names")
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException();

                colNames = ReadStringArray(ref reader);

                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException();

                reader.Read();
            }

            if (reader.GetString() != "data")
                throw new JsonException();

            reader.Read();
            if(reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            string[]? colNamesArr = colNames?.ToArray();
            SpecimenTableColumn ret = colType switch
            {
                SpecimenTableColumnType.Integer => new SpecimenTableColumn<long>(colType, ReadInt64Array(ref reader), null),
                SpecimenTableColumnType.Real => new SpecimenTableColumn<double>(colType, ReadDoubleArray(ref reader), null),
                SpecimenTableColumnType.String => new SpecimenTableColumn<string>(colType, ReadStringArray(ref reader), null),
                SpecimenTableColumnType.Factor => new SpecimenTableColumn<int>(colType, ReadInt32Array(ref reader), colNamesArr),
                SpecimenTableColumnType.Boolean => new SpecimenTableColumn<bool>(colType, ReadBoolArray(ref reader), null),
                SpecimenTableColumnType.Image or SpecimenTableColumnType.Mesh or SpecimenTableColumnType.PointCloud => 
                    new SpecimenTableColumn<ProjectReferenceLink>(colType, ReadLinkArray(ref reader), colNamesArr),
                _ => throw new JsonException()
            };

            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return ret;
        }

        private static List<int> ReadInt32Array(ref Utf8JsonReader reader)
        {
            List<int> list = new List<int>();

            reader.Read();
            while (reader.TokenType == JsonTokenType.Number)
            {
                list.Add(reader.GetInt32());
                reader.Read();
            }

            return list;
        }

        private static List<long> ReadInt64Array(ref Utf8JsonReader reader)
        {
            List<long> list = new List<long>();

            reader.Read();
            while (reader.TokenType == JsonTokenType.Number)
            {
                list.Add(reader.GetInt64());
                reader.Read();
            }

            return list;
        }

        private static List<double> ReadDoubleArray(ref Utf8JsonReader reader)
        {
            List<double> list = new List<double>();

            reader.Read();
            while (reader.TokenType == JsonTokenType.Number)
            {
                list.Add(reader.GetDouble());
                reader.Read();
            }

            return list;
        }

        private static List<ProjectReferenceLink> ReadLinkArray(ref Utf8JsonReader reader)
        {
            List<ProjectReferenceLink> list = new List<ProjectReferenceLink>();

            reader.Read();
            while (reader.TokenType == JsonTokenType.Number)
            {
                list.Add(new ProjectReferenceLink(reader.GetInt32()));
                reader.Read();
            }

            return list;
        }

        private static List<bool> ReadBoolArray(ref Utf8JsonReader reader)
        {
            List<bool> list = new List<bool>();

            reader.Read();
            while (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                list.Add(reader.TokenType == JsonTokenType.True);
                reader.Read();
            }

            return list;
        }

        private static List<string> ReadStringArray(ref Utf8JsonReader reader)
        {
            List<string> list = new List<string>();

            reader.Read();
            while (reader.TokenType == JsonTokenType.String)
            {
                list.Add(reader.GetString() ?? string.Empty);
                reader.Read();
            }

            return list;
        }

        public override void Write(Utf8JsonWriter writer, SpecimenTableColumn value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("type", (int)value.ColumnType);

            if (value.Names is not null)
            {
                writer.WriteStartArray("names");
                foreach (string name in value.Names)
                    writer.WriteStringValue(name);

                writer.WriteEndArray();
            }

            writer.WriteStartArray("data");

            if (value is SpecimenTableColumn<long> longCol)
            {
                foreach (long x in longCol.Data)
                    writer.WriteNumberValue(x);
            }
            else if (value is SpecimenTableColumn<double> doubleCol)
            {
                foreach (double x in doubleCol.Data)
                    writer.WriteNumberValue(x);
            }
            else if (value is SpecimenTableColumn<int> intCol)
            {
                foreach (int x in intCol.Data)
                    writer.WriteNumberValue(x);
            }
            else if (value is SpecimenTableColumn<bool> boolCol)
            {
                foreach (bool x in boolCol.Data)
                    writer.WriteBooleanValue(x);
            }
            else if (value is SpecimenTableColumn<string> stringCol)
            {
                foreach (string x in stringCol.Data)
                    writer.WriteStringValue(x);
            }
            else if (value is SpecimenTableColumn<ProjectReferenceLink> linkCol)
            {
                foreach (ProjectReferenceLink x in linkCol.Data)
                    writer.WriteNumberValue(x.ReferenceIndex);
            }
            else
            {
                throw new JsonException();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
