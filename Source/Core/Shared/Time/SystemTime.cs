namespace PlayGround.Shared.Time;

/// <summary>
/// **시각의 단일 출처.** `Now`는 UTC를 돌려준다.
///
/// 이름이 `Now`인데 UTC인 이유: 이 시스템에서 "지금"은 언제나 UTC 한 가지다.
/// 호출부가 `Now`와 `UtcNow` 중에 고르게 두면 반드시 섞이므로 **선택지를 없앤다.**
/// `DateTime.Now`·`DateTime.UtcNow`·`DateTime.Today` 직접 호출은 금지이고,
/// `SystemTimeUsageTests`가 이를 자동으로 막는다.
///
/// **`Kind`는 항상 `Utc`다.** 이게 API 응답의 ISO-8601에 `Z`를 붙게 하고,
/// 그 `Z` 덕분에 브라우저가 사용자 시간대로 되돌릴 수 있다 — 빠뜨리면 클라이언트가
/// 변환 기준을 알 수 없어 조용히 어긋난다.
///
/// 한국 시각이 필요한 자리(마감일 계산·시즌 연도)는 `PlayGround.Domain`의 `KoreanTime`이 맡는다.
/// </summary>
public static class SystemTime
{
    /// <summary>지금 (UTC). `DateTime.Now`·`DateTime.UtcNow` 대신 쓴다.</summary>
    public static DateTime Now => DateTime.UtcNow;

    /// <summary>오늘 자정 (UTC). `DateTime.Today` 대신 쓴다.</summary>
    public static DateTime Today => DateTime.UtcNow.Date;

    /// <summary>지금 (오프셋 포함). 오프셋이 필요한 외부 연동용.</summary>
    public static DateTimeOffset OffsetNow => DateTimeOffset.UtcNow;
}
