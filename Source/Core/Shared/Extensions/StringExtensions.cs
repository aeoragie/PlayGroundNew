using System.Globalization;

namespace PlayGround.Shared.Extensions;

public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower(CultureInfo.CurrentCulture));
    }

    public static bool TryParseEnum<TEnum>(this string input, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        return Enum.TryParse<TEnum>(input, true, out result);
    }

    public static TEnum ParseEnum<TEnum>(this string input) where TEnum : struct, Enum
    {
        if (!TryParseEnum<TEnum>(input, out var result))
        {
            throw new ArgumentException($"Input string '{input}' cannot be converted to the enum type '{typeof(TEnum).Name}'.", nameof(input));
        }

        return result;
    }

    public static string NullToEmpty(this string? value) => value ?? string.Empty;
}
