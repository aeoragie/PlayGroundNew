using System.Globalization;
using PlayGround.Shared.Time;

namespace PlayGround.Client.Services
{
    /// <summary>
    /// **표시 시간대의 단일 결정권자.** SystemTime(UTC)을 사용자에게 보여줄 벽시계로 바꾸는
    /// 마지막 한 걸음이 전부 여기를 지난다. 저장·비교·전송은 여전히 SystemTime만 쓴다.
    ///
    /// <para>
    /// 기본값은 **브라우저 시간대**다(<see cref="TimeZoneInfo.Local"/> — WASM에서는 기기 설정을 읽는다).
    /// 설정 없이 전 세계 어디서 열어도 자기 시각으로 보이는 것이 기본이어야 하고,
    /// 계정 시간대 설정이 생기면 <see cref="Override"/>만 채우면 된다.
    /// </para>
    ///
    /// <para>
    /// **업무 달력과는 축이 다르다.** 서울 팀이 올린 "8/10 마감"은 방콕에서 봐도 서울 8/10이다
    /// (그건 `PlayGround.Domain`의 `BusinessCalendar`가 판정한다). 여기가 정하는 것은
    /// "이 순간을 이 사람에게 몇 시로 보여줄까"뿐이다.
    /// </para>
    ///
    /// 시간대를 아는 곳을 하나로 좁히려고 `SystemTime`에는 표시 변환을 두지 않았다 —
    /// `TimeBaselineGuardTests`가 다른 곳의 `ToLocalTime()`·`TimeZoneInfo.Local` 사용을 막는다.
    /// </summary>
    public static class DisplayTime
    {
        /// <summary>
        /// 계정 시간대 설정이 생기면 여기에 채운다. null이면 브라우저 시간대를 쓴다.
        /// 이 한 곳만 바뀌고 호출부는 그대로다.
        /// </summary>
        public static TimeZoneInfo? Override { get; set; }

        /// <summary>지금 적용 중인 표시 시간대.</summary>
        public static TimeZoneInfo Zone => Override ?? TimeZoneInfo.Local;

        /// <summary>
        /// UTC 순간 → 표시 벽시계. 캘린더 그리드·월 그룹핑 같은 달력 산술용.
        ///
        /// **`Kind`는 항상 `Unspecified`로 고정한다.** 두 가지 이유다.
        /// - `ConvertTimeFromUtc`는 대상이 기기 시간대일 때만 `Local`을 주고 아니면 `Unspecified`를 준다.
        ///   <see cref="Override"/> 설정 여부에 따라 `Kind`가 달라지면 계약이 흔들린다.
        /// - `Local`·`Utc`로 표식된 값에 누가 `ToUniversalTime()`을 부르면 오프셋이 조용히 샌다.
        ///   `DateTime` 비교는 `Kind`를 무시하므로 그런 실수는 값 테스트로도 잘 안 드러난다.
        /// </summary>
        public static DateTime ToWallClock(this SystemTime utc) =>
            DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc.UtcDateTime, Zone), DateTimeKind.Unspecified);

        /// <summary>표시 문자열 — 화면에 보이는 시각은 이걸로 만든다.</summary>
        public static string Format(this SystemTime utc, string format) =>
            utc.ToWallClock().ToString(format, CultureInfo.InvariantCulture);

        /// <summary>
        /// 픽커가 준 벽시계 입력 → UTC 순간. 입력의 `Kind`는 무시한다 —
        /// 사용자가 고른 값은 "화면에서 본 그 시각"이라는 뜻이고, 기기 설정에 따라 달라지면 안 된다.
        ///
        /// 서머타임 전환에 걸리는 두 경우를 흡수한다.
        /// - **존재하지 않는 시각**(봄, 시계가 건너뛴 구간) — 유효해질 때까지 앞으로 민다
        /// - **두 번 있는 시각**(가을) — .NET이 표준시로 해석한다(먼저 온 쪽)
        /// </summary>
        public static SystemTime FromWallClock(DateTime wallClock)
        {
            TimeZoneInfo zone = Zone;
            DateTime unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);

            // 전환 폭은 지역마다 다르다(보통 1시간, Lord Howe는 30분) — 폭을 가정하지 않고 조금씩 민다
            while (zone.IsInvalidTime(unspecified))
            {
                unspecified = unspecified.AddMinutes(15);
            }

            return new SystemTime(TimeZoneInfo.ConvertTimeToUtc(unspecified, zone));
        }
    }
}
