using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlayGround.Shared.Time;

/// <summary>
/// **시각의 단일 타입.** 이 시스템에서 "순간"은 언제나 UTC 한 가지이고, 그걸 타입으로 강제한다.
/// 코드는 `DateTime`을 몰라도 된다. 시계 읽기는 <see cref="Now"/>, DB·JSON 경계는
/// 핸들러·컨버터가 자동으로 이 타입을 만든다(`TimeBaselineGuardTests`가 `DateTime` 사용을 막는다).
///
/// 어떤 경로로 들어오든 생성자가 UTC로 정규화한다. Kind가 `Unspecified`인 값(DB에서 읽은 값)은
/// 저장 규칙상 UTC이므로 UTC로 표식하고, `Local`이면 UTC로 변환한다. 그래서
/// `new SystemTime(dbValue)`처럼 만들면 실수할 자리가 없다.
///
/// 직렬화는 ISO-8601 `Z`다. 이 `Z` 덕분에 브라우저가 사용자 시간대로 되돌릴 수 있다.
/// 화면 표시는 <see cref="ToLocalString(string)"/>, 한국 달력 값(마감일·시즌)은
/// `PlayGround.Domain`의 `KoreanTime`이 맡는다.
/// </summary>
[JsonConverter(typeof(SystemTimeJsonConverter))]
public readonly struct SystemTime : IEquatable<SystemTime>, IComparable<SystemTime>, IComparable
{
    private readonly DateTime mValue;

    /// <summary>지금 (UTC).</summary>
    public SystemTime()
    {
        mValue = DateTime.UtcNow;
    }

    /// <summary>UTC로 정규화해 감싼다. DB에서 읽은 값(`Unspecified`)은 UTC로 표식한다.</summary>
    public SystemTime(DateTime value)
    {
        mValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    /// <summary>UTC 달력 성분으로 만든다 (테스트·고정 시점용).</summary>
    public SystemTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        : this(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc))
    {
    }

    /// <summary>지금 (UTC).</summary>
    public static SystemTime Now => new(DateTime.UtcNow);

    // `Today`는 일부러 두지 않는다. 이름은 "오늘"인데 값은 **UTC 달력의 오늘**이라,
    // 한국 시각 00~09시에는 사용자가 보는 오늘과 하루 어긋난다. 쓸 자리가 없다:
    // 한국 달력 오늘은 `KoreanTime.Today`(DateOnly), 화면에 보이는 오늘은
    // `SystemTime.Now.ToWallClock().Date`다.

    public static SystemTime MinValue { get; } = new(DateTime.MinValue);

    public static SystemTime MaxValue { get; } = new(DateTime.MaxValue);

    /// <summary>지금 (오프셋 포함). JWT·OAuth·Redis TTL처럼 `DateTimeOffset`을 요구하는 외부 라이브러리 경계용.</summary>
    public static DateTimeOffset OffsetNow => DateTimeOffset.UtcNow;

    /// <summary>
    /// 원시 UTC `DateTime`. **경계(DB 파라미터·JWT·외부 라이브러리) 전용** — 로직에서 쓰지 않는다.
    /// `default(SystemTime)`(Kind 미지정)도 UTC로 표식해 돌려준다.
    /// </summary>
    public DateTime UtcDateTime => DateTime.SpecifyKind(mValue, DateTimeKind.Utc);

    //.// 성분 (UTC 기준)

    public int Year => mValue.Year;
    public int Month => mValue.Month;
    public int Day => mValue.Day;
    public int Hour => mValue.Hour;
    public int Minute => mValue.Minute;
    public int Second => mValue.Second;
    public long Ticks => mValue.Ticks;

    /// <summary>UTC 달력 날짜의 자정.</summary>
    public SystemTime Date => new(mValue.Date);

    /// <summary>UTC 달력 날짜.</summary>
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

    /// <summary>ISO-8601 라운드트립 (`Z` 포함). 로그·디버그용.</summary>
    public override string ToString() => UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    /// <summary>UTC 기준 포맷. 저장 키(yyyyMM 등)·로그처럼 시간대 무관 문자열용.</summary>
    public string ToString(string format) => UtcDateTime.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>UTC 기준 포맷 (문화권 지정). iCal처럼 포맷 규격이 명시적인 곳용.</summary>
    public string ToString(string format, IFormatProvider provider) => UtcDateTime.ToString(format, provider);

}

/// <summary>ISO-8601 문자열과 상호 변환 — 기존 `DateTime`(UTC) 직렬화와 와이어 포맷이 같다.</summary>
public sealed class SystemTimeJsonConverter : JsonConverter<SystemTime>
{
    public override SystemTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, SystemTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime);
}
