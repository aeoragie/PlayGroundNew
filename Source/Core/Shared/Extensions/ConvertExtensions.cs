using System.Diagnostics;
using System.Globalization;
using PlayGround.Shared.Result;

namespace PlayGround.Shared.Extensions;

public static class ConvertExtensions
{

    public static Result<decimal> ToDecimalSafe(this double value)
    {
        if (double.IsNaN(value))
        {
            Debug.Assert(false, "Cannot convert NaN to decimal");
            return Result<decimal>.Error(ErrorCode.InvalidInput, "Cannot convert NaN to decimal.");
        }

        if (double.IsPositiveInfinity(value))
        {
            Debug.Assert(false, "Cannot convert positive infinity to decimal");
            return Result<decimal>.Error(ErrorCode.InvalidInput, "Cannot convert positive infinity to decimal.");
        }

        if (double.IsNegativeInfinity(value))
        {
            Debug.Assert(false, "Cannot convert negative infinity to decimal");
            return Result<decimal>.Error(ErrorCode.InvalidInput, "Cannot convert negative infinity to decimal.");
        }

        if (value > (double)decimal.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds decimal.MaxValue");
            return Result<decimal>.Error(ErrorCode.OutOfRange, $"Value {value} exceeds decimal.MaxValue ({decimal.MaxValue}).");
        }

        if (value < (double)decimal.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below decimal.MinValue");
            return Result<decimal>.Error(ErrorCode.OutOfRange, $"Value {value} is below decimal.MinValue ({decimal.MinValue}).");
        }

        try
        {
            return Result<decimal>.Success(Convert.ToDecimal(value));
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Failed to convert {value} to decimal");
            return Result<decimal>.Error(ErrorCode.OutOfRange, $"Failed to convert {value} to decimal.");
        }
    }

    public static bool TryToDecimalSafe(this double value, out decimal result, out string errorMessage)
    {
        Result<decimal> outcome = value.ToDecimalSafe();
        result = outcome.IsSuccess ? outcome.Value : default;
        errorMessage = outcome.IsSuccess ? string.Empty : outcome.Message;
        return outcome.IsSuccess;
    }

    public static Result<decimal> ToDecimalSafe(this float value)
    {
        if (float.IsNaN(value))
        {
            Debug.Assert(false, "Cannot convert NaN to decimal");
            return Result<decimal>.Error(ErrorCode.InvalidInput, "Cannot convert NaN to decimal.");
        }

        if (float.IsPositiveInfinity(value))
        {
            Debug.Assert(false, "Cannot convert positive infinity to decimal");
            return Result<decimal>.Error(ErrorCode.InvalidInput, "Cannot convert positive infinity to decimal.");
        }

        if (float.IsNegativeInfinity(value))
        {
            Debug.Assert(false, "Cannot convert negative infinity to decimal");
            return Result<decimal>.Error(ErrorCode.InvalidInput, "Cannot convert negative infinity to decimal.");
        }

        if (value > (float)decimal.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds decimal.MaxValue");
            return Result<decimal>.Error(ErrorCode.OutOfRange, $"Value {value} exceeds decimal.MaxValue.");
        }

        if (value < (float)decimal.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below decimal.MinValue");
            return Result<decimal>.Error(ErrorCode.OutOfRange, $"Value {value} is below decimal.MinValue.");
        }

        try
        {
            return Result<decimal>.Success(Convert.ToDecimal(value));
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Failed to convert {value} to decimal");
            return Result<decimal>.Error(ErrorCode.OutOfRange, $"Failed to convert {value} to decimal.");
        }
    }

    public static bool TryToDecimalSafe(this float value, out decimal result, out string errorMessage)
    {
        Result<decimal> outcome = value.ToDecimalSafe();
        result = outcome.IsSuccess ? outcome.Value : default;
        errorMessage = outcome.IsSuccess ? string.Empty : outcome.Message;
        return outcome.IsSuccess;
    }

    public static Result<double> ToDoubleSafe(this decimal value)
    {
        try
        {
            return Result<double>.Success(Convert.ToDouble(value));
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Value {value} cannot be converted to double");
            return Result<double>.Error(ErrorCode.OutOfRange, $"Value {value} cannot be converted to double.");
        }
    }

    public static bool TryToDoubleSafe(this decimal value, out double result, out string errorMessage)
    {
        Result<double> outcome = value.ToDoubleSafe();
        result = outcome.IsSuccess ? outcome.Value : default;
        errorMessage = outcome.IsSuccess ? string.Empty : outcome.Message;
        return outcome.IsSuccess;
    }

    public static Result<float> ToFloatSafe(this decimal value)
    {
        double doubleValue = (double)value;
        if (doubleValue > float.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds float.MaxValue");
            return Result<float>.Error(ErrorCode.OutOfRange, $"Value {value} exceeds float.MaxValue.");
        }

        if (doubleValue < float.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below float.MinValue");
            return Result<float>.Error(ErrorCode.OutOfRange, $"Value {value} is below float.MinValue.");
        }

        try
        {
            return Result<float>.Success(Convert.ToSingle(value));
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Value {value} cannot be converted to float");
            return Result<float>.Error(ErrorCode.OutOfRange, $"Value {value} cannot be converted to float.");
        }
    }

    public static bool TryToFloatSafe(this decimal value, out float result, out string errorMessage)
    {
        Result<float> outcome = value.ToFloatSafe();
        result = outcome.IsSuccess ? outcome.Value : default;
        errorMessage = outcome.IsSuccess ? string.Empty : outcome.Message;
        return outcome.IsSuccess;
    }

    public static Result<int> ToInt32Safe(this decimal value)
    {
        if (value > int.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds int.MaxValue");
            return Result<int>.Error(ErrorCode.OutOfRange, $"Value {value} exceeds int.MaxValue ({int.MaxValue}).");
        }

        if (value < int.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below int.MinValue");
            return Result<int>.Error(ErrorCode.OutOfRange, $"Value {value} is below int.MinValue ({int.MinValue}).");
        }

        try
        {
            return Result<int>.Success(Convert.ToInt32(value));
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Failed to convert {value} to int");
            return Result<int>.Error(ErrorCode.OutOfRange, $"Failed to convert {value} to int.");
        }
    }

    public static bool TryToInt32Safe(this decimal value, out int result, out string errorMessage)
    {
        Result<int> outcome = value.ToInt32Safe();
        result = outcome.IsSuccess ? outcome.Value : default;
        errorMessage = outcome.IsSuccess ? string.Empty : outcome.Message;
        return outcome.IsSuccess;
    }

    public static Result<int> ToInt32Safe(this double value)
    {
        if (double.IsNaN(value))
        {
            Debug.Assert(false, "Cannot convert NaN to int");
            return Result<int>.Error(ErrorCode.InvalidInput, "Cannot convert NaN to int.");
        }

        if (double.IsInfinity(value))
        {
            Debug.Assert(false, "Cannot convert infinity to int");
            return Result<int>.Error(ErrorCode.InvalidInput, "Cannot convert infinity to int.");
        }

        if (value > int.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds int.MaxValue");
            return Result<int>.Error(ErrorCode.OutOfRange, $"Value {value} exceeds int.MaxValue ({int.MaxValue}).");
        }

        if (value < int.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below int.MinValue");
            return Result<int>.Error(ErrorCode.OutOfRange, $"Value {value} is below int.MinValue ({int.MinValue}).");
        }

        try
        {
            return Result<int>.Success(Convert.ToInt32(value));
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Failed to convert {value} to int");
            return Result<int>.Error(ErrorCode.OutOfRange, $"Failed to convert {value} to int.");
        }
    }

    public static bool TryToInt32Safe(this double value, out int result, out string errorMessage)
    {
        Result<int> outcome = value.ToInt32Safe();
        result = outcome.IsSuccess ? outcome.Value : default;
        errorMessage = outcome.IsSuccess ? string.Empty : outcome.Message;
        return outcome.IsSuccess;
    }

    public static Result<long> ToInt64Safe(this decimal value)
    {
        if (value > long.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds long.MaxValue");
            return Result<long>.Error(ErrorCode.OutOfRange, $"Value {value} exceeds long.MaxValue ({long.MaxValue}).");
        }

        if (value < long.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below long.MinValue");
            return Result<long>.Error(ErrorCode.OutOfRange, $"Value {value} is below long.MinValue ({long.MinValue}).");
        }

        try
        {
            return Result<long>.Success(Convert.ToInt64(value));
        }
        catch (OverflowException)
        {
            Debug.Assert(false, $"Failed to convert {value} to long");
            return Result<long>.Error(ErrorCode.OutOfRange, $"Failed to convert {value} to long.");
        }
    }

    public static bool TryToInt64Safe(this decimal value, out long result, out string errorMessage)
    {
        Result<long> outcome = value.ToInt64Safe();
        result = outcome.IsSuccess ? outcome.Value : default;
        errorMessage = outcome.IsSuccess ? string.Empty : outcome.Message;
        return outcome.IsSuccess;
    }

    public static Result<short> ToInt16Safe(this decimal value)
    {
        if (value > short.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds short.MaxValue");
            return Result<short>.Error(ErrorCode.OutOfRange, $"Value {value} exceeds short.MaxValue ({short.MaxValue}).");
        }

        if (value < short.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below short.MinValue");
            return Result<short>.Error(ErrorCode.OutOfRange, $"Value {value} is below short.MinValue ({short.MinValue}).");
        }

        return Result<short>.Success(Convert.ToInt16(value));
    }

    public static Result<byte> ToByteSafe(this decimal value)
    {
        if (value > byte.MaxValue)
        {
            Debug.Assert(false, $"Value {value} exceeds byte.MaxValue");
            return Result<byte>.Error(ErrorCode.OutOfRange, $"Value {value} exceeds byte.MaxValue ({byte.MaxValue}).");
        }

        if (value < byte.MinValue)
        {
            Debug.Assert(false, $"Value {value} is below byte.MinValue");
            return Result<byte>.Error(ErrorCode.OutOfRange, $"Value {value} is below byte.MinValue ({byte.MinValue}).");
        }

        return Result<byte>.Success(Convert.ToByte(value));
    }

    public static Result<decimal> ToDecimalSafe(this double? value)
    {
        if (!value.HasValue)
        {
            Debug.Assert(false, "Cannot convert null to decimal");
            return Result<decimal>.Error(ErrorCode.MissingRequired, "Cannot convert null to decimal.");
        }

        return value.Value.ToDecimalSafe();
    }

    public static Result<decimal?> ToDecimalSafeOrNull(this double? value)
    {
        if (!value.HasValue)
        {
            return Result<decimal?>.Success(null);
        }

        Result<decimal> converted = value.Value.ToDecimalSafe();
        return converted.IsSuccess ? Result<decimal?>.Success(converted.Value) : Result<decimal?>.Failure(converted.ResultData);
    }

    public static Result<decimal> ToDecimalSafe(this float? value)
    {
        if (!value.HasValue)
        {
            Debug.Assert(false, "Cannot convert null to decimal");
            return Result<decimal>.Error(ErrorCode.MissingRequired, "Cannot convert null to decimal.");
        }

        return value.Value.ToDecimalSafe();
    }

    public static Result<decimal?> ToDecimalSafeOrNull(this float? value)
    {
        if (!value.HasValue)
        {
            return Result<decimal?>.Success(null);
        }

        Result<decimal> converted = value.Value.ToDecimalSafe();
        return converted.IsSuccess ? Result<decimal?>.Success(converted.Value) : Result<decimal?>.Failure(converted.ResultData);
    }

    public static Result<int> ToInt32Safe(this decimal? value)
    {
        if (!value.HasValue)
        {
            Debug.Assert(false, "Cannot convert null to int");
            return Result<int>.Error(ErrorCode.MissingRequired, "Cannot convert null to int.");
        }

        return value.Value.ToInt32Safe();
    }

    public static Result<int?> ToInt32SafeOrNull(this decimal? value)
    {
        if (!value.HasValue)
        {
            return Result<int?>.Success(null);
        }

        Result<int> converted = value.Value.ToInt32Safe();
        return converted.IsSuccess ? Result<int?>.Success(converted.Value) : Result<int?>.Failure(converted.ResultData);
    }

    public static Result<decimal> ToDecimalSafe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Assert(false, "Cannot convert null or empty string to decimal");
            return Result<decimal>.Error(ErrorCode.MissingRequired, "Cannot convert null or empty string to decimal.");
        }

        if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
        {
            Debug.Assert(false, $"'{value}' is not a valid decimal format");
            return Result<decimal>.Error(ErrorCode.InvalidFormat, $"'{value}' is not a valid decimal format.");
        }

        return Result<decimal>.Success(result);
    }

    public static bool TryToDecimalSafe(this string value, out decimal result)
    {
        Result<decimal> outcome = value.ToDecimalSafe();
        result = outcome.IsSuccess ? outcome.Value : default;
        return outcome.IsSuccess;
    }

    public static Result<double> ToDoubleSafe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Assert(false, "Cannot convert null or empty string to double");
            return Result<double>.Error(ErrorCode.MissingRequired, "Cannot convert null or empty string to double.");
        }

        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
        {
            Debug.Assert(false, $"'{value}' is not a valid double format");
            return Result<double>.Error(ErrorCode.InvalidFormat, $"'{value}' is not a valid double format.");
        }

        if (double.IsNaN(result))
        {
            Debug.Assert(false, "Parsed value is NaN");
            return Result<double>.Error(ErrorCode.InvalidInput, "Parsed value is NaN.");
        }

        if (double.IsInfinity(result))
        {
            Debug.Assert(false, "Parsed value is infinity");
            return Result<double>.Error(ErrorCode.InvalidInput, "Parsed value is infinity.");
        }

        return Result<double>.Success(result);
    }

    public static bool TryToDoubleSafe(this string value, out double result)
    {
        Result<double> outcome = value.ToDoubleSafe();
        result = outcome.IsSuccess ? outcome.Value : default;
        return outcome.IsSuccess;
    }

    public static Result<int> ToInt32Safe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Assert(false, "Cannot convert null or empty string to int");
            return Result<int>.Error(ErrorCode.MissingRequired, "Cannot convert null or empty string to int.");
        }

        if (!int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
        {
            Debug.Assert(false, $"'{value}' is not a valid int format");
            return Result<int>.Error(ErrorCode.InvalidFormat, $"'{value}' is not a valid int format.");
        }

        return Result<int>.Success(result);
    }

    public static bool TryToInt32Safe(this string value, out int result)
    {
        Result<int> outcome = value.ToInt32Safe();
        result = outcome.IsSuccess ? outcome.Value : default;
        return outcome.IsSuccess;
    }

    public static Result<long> ToInt64Safe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Assert(false, "Cannot convert null or empty string to long");
            return Result<long>.Error(ErrorCode.MissingRequired, "Cannot convert null or empty string to long.");
        }

        if (!long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out long result))
        {
            Debug.Assert(false, $"'{value}' is not a valid long format");
            return Result<long>.Error(ErrorCode.InvalidFormat, $"'{value}' is not a valid long format.");
        }

        return Result<long>.Success(result);
    }

    public static bool TryToInt64Safe(this string value, out long result)
    {
        Result<long> outcome = value.ToInt64Safe();
        result = outcome.IsSuccess ? outcome.Value : default;
        return outcome.IsSuccess;
    }

    public static Result<decimal> RoundSafe(this decimal value, int decimals = 2)
    {
        if (decimals < 0 || decimals > 28)
        {
            Debug.Assert(false, $"Decimal places must be between 0 and 28. Provided: {decimals}");
            return Result<decimal>.Error(ErrorCode.OutOfRange, $"Decimal places must be between 0 and 28. Provided: {decimals}");
        }

        return Result<decimal>.Success(Math.Round(value, decimals, MidpointRounding.AwayFromZero));
    }

    public static Result<decimal> RoundToDecimalSafe(this double value, int decimals = 2)
    {
        Result<decimal> converted = value.ToDecimalSafe();
        return converted.IsSuccess ? converted.Value.RoundSafe(decimals) : converted;
    }

    public static Result<double> RoundSafe(this double value, int decimals = 2)
    {
        if (double.IsNaN(value))
        {
            Debug.Assert(false, "Cannot round NaN value");
            return Result<double>.Error(ErrorCode.InvalidInput, "Cannot round NaN value.");
        }

        if (double.IsInfinity(value))
        {
            Debug.Assert(false, "Cannot round infinity value");
            return Result<double>.Error(ErrorCode.InvalidInput, "Cannot round infinity value.");
        }

        if (decimals < 0 || decimals > 15)
        {
            Debug.Assert(false, $"Decimal places must be between 0 and 15 for double. Provided: {decimals}");
            return Result<double>.Error(ErrorCode.OutOfRange, $"Decimal places must be between 0 and 15 for double. Provided: {decimals}");
        }

        return Result<double>.Success(Math.Round(value, decimals, MidpointRounding.AwayFromZero));
    }

    public static decimal CeilingSafe(this decimal value) => Math.Ceiling(value);

    public static decimal FloorSafe(this decimal value) => Math.Floor(value);

    public static decimal TruncateSafe(this decimal value) => Math.Truncate(value);

    public static Result<decimal> AverageToDecimalSafe<TItem>(this IEnumerable<TItem> source, Func<TItem, double> selector)
    {
        if (source == null)
        {
            Debug.Assert(false, "Source cannot be null");
            return Result<decimal>.Error(ErrorCode.MissingRequired, "Source cannot be null.");
        }

        if (selector == null)
        {
            Debug.Assert(false, "Selector cannot be null");
            return Result<decimal>.Error(ErrorCode.MissingRequired, "Selector cannot be null.");
        }

        List<TItem> list = source.ToList();
        if (list.Count == 0)
        {
            Debug.Assert(false, "Cannot calculate average of empty collection");
            return Result<decimal>.Error(ErrorCode.InvalidOperation, "Cannot calculate average of empty collection.");
        }

        return list.Average(selector).ToDecimalSafe();
    }

    public static decimal AverageToDecimalSafeOrDefault<TItem>(this IEnumerable<TItem> source, Func<TItem, double> selector, decimal defaultValue = 0m)
    {
        Result<decimal> outcome = source.AverageToDecimalSafe(selector);
        return outcome.IsSuccess ? outcome.Value : defaultValue;
    }

    public static Result<decimal> SumToDecimalSafe<TItem>(this IEnumerable<TItem> source, Func<TItem, double> selector)
    {
        if (source == null)
        {
            Debug.Assert(false, "Source cannot be null");
            return Result<decimal>.Error(ErrorCode.MissingRequired, "Source cannot be null.");
        }

        if (selector == null)
        {
            Debug.Assert(false, "Selector cannot be null");
            return Result<decimal>.Error(ErrorCode.MissingRequired, "Selector cannot be null.");
        }

        return source.Sum(selector).ToDecimalSafe();
    }

    public static decimal SumToDecimalSafeOrDefault<TItem>(this IEnumerable<TItem> source, Func<TItem, double> selector, decimal defaultValue = 0m)
    {
        Result<decimal> outcome = source.SumToDecimalSafe(selector);
        return outcome.IsSuccess ? outcome.Value : defaultValue;
    }

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

    public static Result<decimal> CalculatePercentage(this decimal value, decimal total)
    {
        if (total == 0)
        {
            Debug.Assert(false, "Cannot calculate percentage with zero total");
            return Result<decimal>.Error(ErrorCode.InvalidInput, "Cannot calculate percentage with zero total.");
        }

        return (value / total * 100m).RoundSafe(2);
    }

    public static decimal CalculatePercentageOrDefault(this decimal value, decimal total, decimal defaultValue = 0m)
    {
        if (total == 0)
        {
            return defaultValue;
        }

        Result<decimal> outcome = (value / total * 100m).RoundSafe(2);
        return outcome.IsSuccess ? outcome.Value : defaultValue;
    }
    public static decimal ToDecimalFraction(this decimal percentage) => percentage / 100m;
    public static Result<decimal> ToPercentage(this decimal fraction) => (fraction * 100m).RoundSafe(2);
}
