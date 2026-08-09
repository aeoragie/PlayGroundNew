using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlayGround.Shared.Http
{
    /// <summary>
    /// enum ↔ 이름 문자열. 모르는 값은 예외 대신 기본 멤버(Unknown)로 받는다 —
    /// 서버가 멤버를 추가해도 캐시된 옛 클라이언트의 역직렬화가 통째로 죽지 않는다.
    /// </summary>
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
