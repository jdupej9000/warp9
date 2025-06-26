using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.JsonConverters
{
    public class LutSpecJsonConverter : JsonConverter<LutSpec>
    {
        public override LutSpec? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int q = 0;
            List<float> stops = new List<float>();
            List<Color> colors = new List<Color>();

            while (true)
            {
                reader.Read();
                if(reader.TokenType != JsonTokenType.PropertyName)
                    break;

                string key = reader.GetString() ?? "";
                if (key == "q")
                {
                    reader.Read();
                    q = reader.GetInt32();
                }
                else if (key == "pos")
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException();

                    reader.Read();
                    while (reader.TokenType == JsonTokenType.Number)
                    {
                        stops.Add(reader.GetSingle());
                        reader.Read();
                    }

                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new JsonException();
                }
                else if (key == "col")
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException();

                    reader.Read();
                    while (reader.TokenType == JsonTokenType.String)
                    {
                        colors.Add(ColorTranslator.FromHtml(reader.GetString() ?? "#0"));
                        reader.Read();
                    }

                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new JsonException();
                }
                else
                {
                    // Make provisions for future simple values.
                    reader.Read();
                }

            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return new LutSpec(q, stops, colors);
        }

        public override void Write(Utf8JsonWriter writer, LutSpec value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value.IsQuantized)
                writer.WriteNumber("q", value.NumSegments);

            int n = value.NumStops;
            
            writer.WriteStartArray("pos");
            for (int i = 0; i < n; i++)
                writer.WriteNumberValue(value.StopPos[i]);
            writer.WriteEndArray();

            writer.WriteStartArray("col");
            for (int i = 0; i < n; i++)
                writer.WriteStringValue(ColorTranslator.ToHtml(value.StopColors[i]));
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
