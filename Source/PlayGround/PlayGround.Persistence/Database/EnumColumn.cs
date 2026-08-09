using PlayGround.Shared.Primitives;

namespace PlayGround.Persistence.Database
{
    /// <summary>enum ↔ DB 문자열 변환은 여기 하나다. 저장 값이 멤버 이름이 아니면 데이터 버그 — Panic.</summary>
    internal static class EnumColumn
    {
        public static TEnum? Read<TEnum>(string? value) where TEnum : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (Enum.TryParse(value, out TEnum parsed) && Enum.IsDefined(parsed) && !parsed.Equals(default(TEnum)))
            {
                return parsed;
            }

            return Panic.Fail<TEnum?>($"{typeof(TEnum).Name} column holds invalid value '{value}'.");
        }

        public static TEnum ReadRequired<TEnum>(string value) where TEnum : struct, Enum
        {
            TEnum? parsed = Read<TEnum>(value);
            if (parsed is null)
            {
                return Panic.Fail<TEnum>($"{typeof(TEnum).Name} column is empty but required.");
            }

            return parsed.Value;
        }

        public static string? Write<TEnum>(TEnum? value) where TEnum : struct, Enum
        {
            if (value is null)
            {
                return null;
            }

            if (value.Value.Equals(default(TEnum)))
            {
                return Panic.Fail<string?>($"{typeof(TEnum).Name}.Unknown cannot be stored.");
            }

            return value.Value.ToString();
        }
    }
}
