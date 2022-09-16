using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoralTime.Services
{
    public class TrimmingStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();

                    reader.Read();

                    if (!propertyName.Equals("password", StringComparison.InvariantCultureIgnoreCase))
                        return reader.GetString().Trim();
                }
            }

            return reader.GetString().Trim();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
