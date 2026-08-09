using PlayGround.Shared.Primitives;

namespace PlayGround.Shared.Extensions;

/// <summary>enum ↔ 저장 문자열 변환은 여기 하나다. 기본값(0=미지정) ↔ NULL, 그 외 비정형 저장 값은 데이터 버그 — Panic.
/// 신뢰된 저장소(우리가 쓴 DB)를 읽는 경로 전용 — 사용자 입력·와이어는 관대한 파서(LenientEnumJsonConverter)가 담당한다.</summary>
public static class EnumColumn
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
