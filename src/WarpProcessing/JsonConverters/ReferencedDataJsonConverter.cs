using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Model;

namespace Warp9.JsonConverters
{
    public class ReferencedDataJsonConverter<T> : JsonConverter<ReferencedData<T>> 
        where T : class
    {
        public override ReferencedData<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
                throw new JsonException();

            return new ReferencedData<T>(reader.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, ReferencedData<T> value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Key);
        }
    }
}
