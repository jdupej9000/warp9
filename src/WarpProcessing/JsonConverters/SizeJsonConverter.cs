using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.JsonConverters
{
    public class SizeJsonConverter : JsonConverter<Size>
    {
        public override Size Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int w = 0, h = 0;

            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            reader.Read();
            while (reader.TokenType == JsonTokenType.PropertyName) 
            {
                string prop = reader.GetString() ?? string.Empty;
                reader.Read();
                if (reader.TokenType != JsonTokenType.Number)
                    throw new JsonException();

                switch (prop)
                {
                    case "w":                        
                        w = reader.GetInt32();
                        break;

                    case "h":
                        h = reader.GetInt32();
                        break;
                }

                reader.Read();
            }

            if(reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return new Size(w, h);
        }

        public override void Write(Utf8JsonWriter writer, Size value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteNumber("w", value.Width);
            writer.WriteNumber("h", value.Height);

            writer.WriteEndObject();
        }
    }
}
