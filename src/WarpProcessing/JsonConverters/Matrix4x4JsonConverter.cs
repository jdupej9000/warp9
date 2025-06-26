using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.JsonConverters
{
    public class Matrix4x4JsonConverter : JsonConverter<Matrix4x4>
    {
        public override Matrix4x4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Matrix4x4 ret = default;

            reader.Read();
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    reader.Read();
                    if(reader.TokenType != JsonTokenType.Number)
                        throw new JsonException();

                    ret[j, i] = reader.GetSingle();
                }
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();

            return ret;
        }

        public override void Write(Utf8JsonWriter writer, Matrix4x4 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                    writer.WriteNumberValue(value[j, i]);
            }

            writer.WriteEndArray();            
        }
    }
}
