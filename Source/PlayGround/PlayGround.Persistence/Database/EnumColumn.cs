using PlayGround.Shared.Primitives;

namespace PlayGround.Persistence.Database
{
    /// <summary>enum ↔ DB 문자열 변환은 여기 하나다. Unknown ↔ NULL, 그 외 비정형 저장 값은 데이터 버그 — Panic.</summary>
    internal static class EnumColumn
    {
        public static TEnum Read<TEnum>(string? value) where TEnum : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            if (Enum.TryParse(value, out TEnum parsed) && Enum.IsDefined(parsed) && !parsed.Equals(default(TEnum)))
            {
                return parsed;
            }

            return Panic.Fail<TEnum>($"{typeof(TEnum).Name} column holds invalid value '{value}'.");
        }

        public static string? Write<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            return value.Equals(default(TEnum)) ? null : value.ToString();
        }
    }
}
