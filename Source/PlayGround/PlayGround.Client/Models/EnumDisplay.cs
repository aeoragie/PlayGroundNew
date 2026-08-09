namespace PlayGround.Client.Models
{
    /// <summary>enum 표시 공통 — Unknown(0)은 값이 아니라 미지정이므로 null을 돌려 "-"·생략 폴백에 잇는다.</summary>
    public static class EnumDisplay
    {
        public static string? ToText<TEnum>(this TEnum value) where TEnum : struct, Enum
        {
            return value.Equals(default(TEnum)) ? null : value.ToString();
        }
    }
}
