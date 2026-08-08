using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlayGround.Shared.Time;

/// <summary>
/// 시각의 단일 타입. "순간"은 언제나 UTC 하나이고 그걸 타입으로 강제한다 — 어떤 Kind로 들어와도
/// 생성자가 UTC로 정규화하고, DB(Dapper 핸들러)·JSON(ISO-8601 Z) 경계는 자동 변환이라 로직은
/// <c>DateTime</c>을 모른다. 시간대는 이 타입이 아니라 Client의 <c>DisplayTime</c>이 안다.
/// </summary>
[JsonConverter(typeof(SystemTimeJsonConverter))]
public readonly struct SystemTime : IEquatable<SystemTime>, IComparable<SystemTime>, IComparable
{
    private readonly DateTime mValue;

    public SystemTime()
    {
        mValue = DateTime.UtcNow;
    }

    /// <summary>DB에서 읽은 값(<c>Unspecified</c>)은 저장 규칙상 UTC이므로 그대로 UTC로 표식한다.</summary>
    public SystemTime(DateTime value)
    {
        mValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public SystemTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        : this(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc))
    {
    }

    public static SystemTime Now => new(DateTime.UtcNow + DebugClock.Offset);

    // Today는 일부러 두지 않는다 — UTC 달력의 오늘이라 보는 사람의 오늘과 하루 어긋난다.

    public static SystemTime MinValue { get; } = new(DateTime.MinValue);

    public static SystemTime MaxValue { get; } = new(DateTime.MaxValue);

    /// <summary>JWT·OAuth·Redis TTL처럼 <c>DateTimeOffset</c>을 요구하는 외부 라이브러리 경계용.</summary>
    public static DateTimeOffset OffsetNow => DateTimeOffset.UtcNow;

    /// <summary>원시 UTC <c>DateTime</c>. 경계(DB 파라미터·외부 라이브러리) 전용 — 로직에서 쓰지 않는다.</summary>
    public DateTime UtcDateTime => DateTime.SpecifyKind(mValue, DateTimeKind.Utc);

    //.// 성분 (UTC 기준)

    public int Year => mValue.Year;
    public int Month => mValue.Month;
    public int Day => mValue.Day;
    public int Hour => mValue.Hour;
    public int Minute => mValue.Minute;
    public int Second => mValue.Second;
    public long Ticks => mValue.Ticks;

    public SystemTime Date => new(mValue.Date);

    public DateOnly DateOnly => DateOnly.FromDateTime(UtcDateTime);

    //.// 산술

    public SystemTime Add(TimeSpan value) => new(UtcDateTime.Add(value));
    public SystemTime AddDays(double value) => new(UtcDateTime.AddDays(value));
    public SystemTime AddHours(double value) => new(UtcDateTime.AddHours(value));
    public SystemTime AddMinutes(double value) => new(UtcDateTime.AddMinutes(value));
    public SystemTime AddSeconds(double value) => new(UtcDateTime.AddSeconds(value));
    public SystemTime AddMonths(int value) => new(UtcDateTime.AddMonths(value));
    public SystemTime AddYears(int value) => new(UtcDateTime.AddYears(value));
    public SystemTime AddTicks(long value) => new(UtcDateTime.AddTicks(value));

    public static TimeSpan operator -(SystemTime left, SystemTime right) => left.UtcDateTime - right.UtcDateTime;
    public static SystemTime operator +(SystemTime time, TimeSpan span) => time.Add(span);
    public static SystemTime operator -(SystemTime time, TimeSpan span) => time.Add(-span);

    //.// 비교

    public static bool operator ==(SystemTime left, SystemTime right) => left.mValue == right.mValue;
    public static bool operator !=(SystemTime left, SystemTime right) => left.mValue != right.mValue;
    public static bool operator <(SystemTime left, SystemTime right) => left.mValue < right.mValue;
    public static bool operator <=(SystemTime left, SystemTime right) => left.mValue <= right.mValue;
    public static bool operator >(SystemTime left, SystemTime right) => left.mValue > right.mValue;
    public static bool operator >=(SystemTime left, SystemTime right) => left.mValue >= right.mValue;

    public bool Equals(SystemTime other) => mValue == other.mValue;
    public override bool Equals(object? obj) => obj is SystemTime other && Equals(other);
    public override int GetHashCode() => mValue.GetHashCode();
    public int CompareTo(SystemTime other) => mValue.CompareTo(other.mValue);

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        SystemTime other => CompareTo(other),
        _ => throw new ArgumentException($"Cannot compare SystemTime with {obj.GetType().Name}"),
    };

    //.// 표시

    public override string ToString() => UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    public string ToString(string format) => UtcDateTime.ToString(format, CultureInfo.InvariantCulture);

    public string ToString(string format, IFormatProvider provider) => UtcDateTime.ToString(format, provider);

}

public sealed class SystemTimeJsonConverter : JsonConverter<SystemTime>
{
    public override SystemTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, SystemTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime);
}
