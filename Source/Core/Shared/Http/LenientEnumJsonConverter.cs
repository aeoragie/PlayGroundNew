using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlayGround.Shared.Http
{
    public sealed class LenientEnumJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                return default;
            }

            string? value = reader.GetString();
            if (Enum.TryParse(value, out TEnum parsed) && Enum.IsDefined(parsed))
            {
                return parsed;
            }

            return default;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
