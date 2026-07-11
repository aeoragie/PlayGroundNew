using System.Diagnostics;
using System.Globalization;

namespace PlayGround.Shared.Extensions;

public static class ConvertExtensions
{
    #region Double to Decimal Conversions

    public static decimal ToDecimalSafe(this double value)
    {
        if (double.IsNaN(value))
        {
            Debug.Assert(false, "Cannot convert NaN to decimal");
            throw new InvalidCastException("Cannot convert NaN to decimal.");
        }

        if (double.IsPositiveInfinity(value))
        {
            Debug.Assert(false, "Cannot convert positive infinity to decimal");
            throw new InvalidCastException("Cannot convert positive infinity to decimal.");
        }

        if (double.IsNegativeInfinity(value))
        {
            Debug.Assert(false, "Cannot convert negative infinity to decimal");
            throw new InvalidCastException("Cannot convert negative infinity to decimal.");
        }

        if (value > (double)decimal.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds decimal.MaxValue");
            throw new OverflowException($"Value {value} exceeds decimal.MaxValue ({decimal.MaxValue}).");
        }

        if (value < (double)decimal.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below decimal.MinValue");
            throw new OverflowException($"Value {value} is below decimal.MinValue ({decimal.MinValue}).");
        }

        try
        {
            return Convert.ToDecimal(value);
        }
        catch (OverflowException ex)
        {
            Debug.Assert(false, $"Failed to convert {value} to decimal");
            throw new OverflowException($"Failed to convert {value} to decimal.", ex);
        }
    }

    public static bool TryToDecimalSafe(this double value, out decimal result, out string errorMessage)
    {
        result = 0m;
        errorMessage = string.Empty;

        try
        {
            result = value.ToDecimalSafe();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    #endregion

    #region Float to Decimal Conversions

    public static decimal ToDecimalSafe(this float value)
    {
        if (float.IsNaN(value))
        {
            Debug.Assert(false, "Cannot convert NaN to decimal");
            throw new InvalidCastException("Cannot convert NaN to decimal.");
        }

        if (float.IsPositiveInfinity(value))
        {
            Debug.Assert(false, "Cannot convert positive infinity to decimal");
            throw new InvalidCastException("Cannot convert positive infinity to decimal.");
        }

        if (float.IsNegativeInfinity(value))
        {
            Debug.Assert(false, "Cannot convert negative infinity to decimal");
            throw new InvalidCastException("Cannot convert negative infinity to decimal.");
        }

        if (value > (float)decimal.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds decimal.MaxValue");
            throw new OverflowException($"Value {value} exceeds decimal.MaxValue.");
        }

        if (value < (float)decimal.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below decimal.MinValue");
            throw new OverflowException($"Value {value} is below decimal.MinValue.");
        }

        try
        {
            return Convert.ToDecimal(value);
        }
        catch (OverflowException ex)
        {
            Debug.Assert(false, $"Failed to convert {value} to decimal");
            throw new OverflowException($"Failed to convert {value} to decimal.", ex);
        }
    }

    public static bool TryToDecimalSafe(this float value, out decimal result, out string errorMessage)
    {
        result = 0m;
        errorMessage = string.Empty;

        try
        {
            result = value.ToDecimalSafe();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    #endregion

    #region Decimal to Other Types

    public static double ToDoubleSafe(this decimal value)
    {
        try
        {
            return Convert.ToDouble(value);
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Value {value} cannot be converted to double");
            throw new OverflowException($"Value {value} cannot be converted to double.");
        }
    }

    public static bool TryToDoubleSafe(this decimal value, out double result, out string errorMessage)
    {
        result = 0d;
        errorMessage = string.Empty;

        try
        {
            result = value.ToDoubleSafe();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static float ToFloatSafe(this decimal value)
    {
        double doubleValue = (double)value;
        if (doubleValue > float.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds float.MaxValue");
            throw new OverflowException($"Value {value} exceeds float.MaxValue.");
        }

        if (doubleValue < float.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below float.MinValue");
            throw new OverflowException($"Value {value} is below float.MinValue.");
        }

        try
        {
            return Convert.ToSingle(value);
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Value {value} cannot be converted to float");
            throw new OverflowException($"Value {value} cannot be converted to float.");
        }
    }

    public static bool TryToFloatSafe(this decimal value, out float result, out string errorMessage)
    {
        result = 0f;
        errorMessage = string.Empty;

        try
        {
            result = value.ToFloatSafe();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    #endregion

    #region Integer Conversions

    public static int ToInt32Safe(this decimal value)
    {
        if (value > int.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds int.MaxValue");
            throw new OverflowException($"Value {value} exceeds int.MaxValue ({int.MaxValue}).");
        }

        if (value < int.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below int.MinValue");
            throw new OverflowException($"Value {value} is below int.MinValue ({int.MinValue}).");
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch (OverflowException ex)
        {
            Debug.Assert(false, $"Failed to convert {value} to int");
            throw new OverflowException($"Failed to convert {value} to int.", ex);
        }
    }

    public static bool TryToInt32Safe(this decimal value, out int result, out string errorMessage)
    {
        result = 0;
        errorMessage = string.Empty;

        try
        {
            result = value.ToInt32Safe();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static int ToInt32Safe(this double value)
    {
        if (double.IsNaN(value))
        {
            Debug.Assert(false, "Cannot convert NaN to int");
            throw new InvalidCastException("Cannot convert NaN to int.");
        }

        if (double.IsInfinity(value))
        {
            Debug.Assert(false, "Cannot convert infinity to int");
            throw new InvalidCastException("Cannot convert infinity to int.");
        }

        if (value > int.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds int.MaxValue");
            throw new OverflowException($"Value {value} exceeds int.MaxValue ({int.MaxValue}).");
        }

        if (value < int.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below int.MinValue");
            throw new OverflowException($"Value {value} is below int.MinValue ({int.MinValue}).");
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch (OverflowException ex)
        {
            Debug.Assert(false, $"Failed to convert {value} to int");
            throw new OverflowException($"Failed to convert {value} to int.", ex);
        }
    }

    public static bool TryToInt32Safe(this double value, out int result, out string errorMessage)
    {
        result = 0;
        errorMessage = string.Empty;

        try
        {
            result = value.ToInt32Safe();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static long ToInt64Safe(this decimal value)
    {
        if (value > long.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds long.MaxValue");
            throw new OverflowException($"Value {value} exceeds long.MaxValue ({long.MaxValue}).");
        }

        if (value < long.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below long.MinValue");
            throw new OverflowException($"Value {value} is below long.MinValue ({long.MinValue}).");
        }

        try
        {
            return Convert.ToInt64(value);
        }
        catch (OverflowException ex)
        {
            Debug.Assert(false, $"Failed to convert {value} to long");
            throw new OverflowException($"Failed to convert {value} to long.", ex);
        }
    }

    public static bool TryToInt64Safe(this decimal value, out long result, out string errorMessage)
    {
        result = 0L;
        errorMessage = string.Empty;

        try
        {
            result = value.ToInt64Safe();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static short ToInt16Safe(this decimal value)
    {
        if (value > short.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds short.MaxValue");
            throw new OverflowException($"Value {value} exceeds short.MaxValue ({short.MaxValue}).");
        }

        if (value < short.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below short.MinValue");
            throw new OverflowException($"Value {value} is below short.MinValue ({short.MinValue}).");
        }

        return Convert.ToInt16(value);
    }

    public static byte ToByteSafe(this decimal value)
    {
        if (value > byte.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds byte.MaxValue");
            throw new OverflowException($"Value {value} exceeds byte.MaxValue ({byte.MaxValue}).");
        }

        if (value < byte.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below byte.MinValue");
            throw new OverflowException($"Value {value} is below byte.MinValue ({byte.MinValue}).");
        }

        return Convert.ToByte(value);
    }

    #endregion

    #region Nullable Conversions

    public static decimal ToDecimalSafe(this double? value)
    {
        if (!value.HasValue)
        {
            Debug.Assert(false, "Cannot convert null to decimal");
            throw new ArgumentNullException(nameof(value), "Cannot convert null to decimal.");
        }

        return value.Value.ToDecimalSafe();
    }

    public static decimal? ToDecimalSafeOrNull(this double? value)
    {
        return value.HasValue ? value.Value.ToDecimalSafe() : null;
    }

    public static decimal ToDecimalSafe(this float? value)
    {
        if (!value.HasValue)
        {
            Debug.Assert(false, "Cannot convert null to decimal");
            throw new ArgumentNullException(nameof(value), "Cannot convert null to decimal.");
        }

        return value.Value.ToDecimalSafe();
    }

    public static decimal? ToDecimalSafeOrNull(this float? value)
    {
        return value.HasValue ? value.Value.ToDecimalSafe() : null;
    }

    public static int ToInt32Safe(this decimal? value)
    {
        if (!value.HasValue)
        {
            Debug.Assert(false, "Cannot convert null to int");
            throw new ArgumentNullException(nameof(value), "Cannot convert null to int.");
        }

        return value.Value.ToInt32Safe();
    }

    public static int? ToInt32SafeOrNull(this decimal? value)
    {
        return value.HasValue ? value.Value.ToInt32Safe() : null;
    }

    #endregion

    #region String to Numeric Conversions

    public static decimal ToDecimalSafe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Assert(false, "Cannot convert null or empty string to decimal");
            throw new ArgumentNullException(nameof(value), "Cannot convert null or empty string to decimal.");
        }

        if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
        {
            Debug.Assert(false, $"'{value}' is not a valid decimal format");
            throw new FormatException($"'{value}' is not a valid decimal format.");
        }

        return result;
    }

    public static bool TryToDecimalSafe(this string value, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static double ToDoubleSafe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Assert(false, "Cannot convert null or empty string to double");
            throw new ArgumentNullException(nameof(value), "Cannot convert null or empty string to double.");
        }

        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
        {
            Debug.Assert(false, $"'{value}' is not a valid double format");
            throw new FormatException($"'{value}' is not a valid double format.");
        }

        if (double.IsNaN(result))
        {
            Debug.Assert(false, "Parsed value is NaN");
            throw new InvalidCastException("Parsed value is NaN.");
        }

        if (double.IsInfinity(result))
        {
            Debug.Assert(false, "Parsed value is infinity");
            throw new InvalidCastException("Parsed value is infinity.");
        }

        return result;
    }

    public static bool TryToDoubleSafe(this string value, out double result)
    {
        result = 0d;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            return false;
        }

        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            result = 0d;
            return false;
        }

        return true;
    }

    public static int ToInt32Safe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Assert(false, "Cannot convert null or empty string to int");
            throw new ArgumentNullException(nameof(value), "Cannot convert null or empty string to int.");
        }

        if (!int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
        {
            Debug.Assert(false, $"'{value}' is not a valid int format");
            throw new FormatException($"'{value}' is not a valid int format.");
        }

        return result;
    }

    public static bool TryToInt32Safe(this string value, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static long ToInt64Safe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Assert(false, "Cannot convert null or empty string to long");
            throw new ArgumentNullException(nameof(value), "Cannot convert null or empty string to long.");
        }

        if (!long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out long result))
        {
            Debug.Assert(false, $"'{value}' is not a valid long format");
            throw new FormatException($"'{value}' is not a valid long format.");
        }

        return result;
    }

    public static bool TryToInt64Safe(this string value, out long result)
    {
        result = 0L;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    #endregion

    #region Rounding Helpers

    public static decimal RoundSafe(this decimal value, int decimals = 2)
    {
        if (decimals < 0 || decimals > 28)
        {
            Debug.Assert(false, $"Decimal places must be between 0 and 28. Provided: {decimals}");
            throw new ArgumentOutOfRangeException(nameof(decimals), $"Decimal places must be between 0 and 28. Provided: {decimals}");
        }

        return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    public static decimal RoundToDecimalSafe(this double value, int decimals = 2)
    {
        return value.ToDecimalSafe().RoundSafe(decimals);
    }

    public static double RoundSafe(this double value, int decimals = 2)
    {
        if (double.IsNaN(value))
        {
            Debug.Assert(false, "Cannot round NaN value");
            throw new InvalidCastException("Cannot round NaN value.");
        }

        if (double.IsInfinity(value))
        {
            Debug.Assert(false, "Cannot round infinity value");
            throw new InvalidCastException("Cannot round infinity value.");
        }

        if (decimals < 0 || decimals > 15)
        {
            Debug.Assert(false, $"Decimal places must be between 0 and 15 for double. Provided: {decimals}");
            throw new ArgumentOutOfRangeException(nameof(decimals), $"Decimal places must be between 0 and 15 for double. Provided: {decimals}");
        }

        return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    public static decimal CeilingSafe(this decimal value) => Math.Ceiling(value);

    public static decimal FloorSafe(this decimal value) => Math.Floor(value);

    public static decimal TruncateSafe(this decimal value) => Math.Truncate(value);

    #endregion

    #region Collection Extensions

    public static decimal AverageToDecimalSafe<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        if (source == null)
        {
            Debug.Assert(false, "Source cannot be null");
            throw new ArgumentNullException(nameof(source));
        }

        if (selector == null)
        {
            Debug.Assert(false, "Selector cannot be null");
            throw new ArgumentNullException(nameof(selector));
        }

        var list = source.ToList();
        if (!list.Any())
        {
            Debug.Assert(false, "Cannot calculate average of empty collection");
            throw new InvalidOperationException("Cannot calculate average of empty collection.");
        }

        return list.Average(selector).ToDecimalSafe();
    }

    public static decimal AverageToDecimalSafeOrDefault<T>(this IEnumerable<T> source, Func<T, double> selector, decimal defaultValue = 0m)
    {
        if (source == null || selector == null)
        {
            return defaultValue;
        }

        var list = source.ToList();
        if (!list.Any())
        {
            return defaultValue;
        }

        try
        {
            return list.Average(selector).ToDecimalSafe();
        }
        catch
        {
            return defaultValue;
        }
    }

    public static decimal SumToDecimalSafe<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        if (source == null)
        {
            Debug.Assert(false, "Source cannot be null");
            throw new ArgumentNullException(nameof(source));
        }

        if (selector == null)
        {
            Debug.Assert(false, "Selector cannot be null");
            throw new ArgumentNullException(nameof(selector));
        }

        return source.Sum(selector).ToDecimalSafe();
    }

    public static decimal SumToDecimalSafeOrDefault<T>(this IEnumerable<T> source, Func<T, double> selector, decimal defaultValue = 0m)
    {
        if (source == null || selector == null)
        {
            return defaultValue;
        }

        try
        {
            return source.Sum(selector).ToDecimalSafe();
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion

    #region Validation Helpers

    public static bool CanConvertToDecimal(this double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value)
            && value <= (double)decimal.MaxValue && value >= (double)decimal.MinValue;
    }

    public static bool CanConvertToDecimal(this float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value)
            && value <= (float)decimal.MaxValue && value >= (float)decimal.MinValue;
    }

    public static bool CanConvertToInt32(this double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value)
            && value <= int.MaxValue && value >= int.MinValue;
    }

    public static bool CanConvertToInt32(this decimal value)
    {
        return value >= int.MinValue && value <= int.MaxValue;
    }

    public static bool CanConvertToInt64(this decimal value)
    {
        return value >= long.MinValue && value <= long.MaxValue;
    }

    public static bool IsValidDecimal(this string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    public static bool IsValidDouble(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
        {
            return false;
        }

        return !double.IsNaN(result) && !double.IsInfinity(result);
    }

    public static bool IsValidInt32(this string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    #endregion

    #region Percentage Helpers

    public static decimal CalculatePercentage(this decimal value, decimal total)
    {
        if (total == 0)
        {
            Debug.Assert(false, "Cannot calculate percentage with zero total");
            throw new DivideByZeroException("Cannot calculate percentage with zero total.");
        }

        return (value / total * 100m).RoundSafe(2);
    }

    public static decimal CalculatePercentageOrDefault(this decimal value, decimal total, decimal defaultValue = 0m)
    {
        return total == 0 ? defaultValue : (value / total * 100m).RoundSafe(2);
    }

    /// <summary>
    /// 백분율 -> 소수 (50 -> 0.5)
    /// </summary>
    public static decimal ToDecimalFraction(this decimal percentage) => percentage / 100m;

    /// <summary>
    /// 소수 -> 백분율 (0.5 -> 50)
    /// </summary>
    public static decimal ToPercentage(this decimal fraction) => (fraction * 100m).RoundSafe(2);

    #endregion
}
