using PlayGround.Shared.Time;
using System.Globalization;

namespace PlayGround.Client.Services
{
    /// <summary>
    /// 표시 시간대의 단일 결정권자. SystemTime(UTC)을 사용자에게 보여줄 벽시계로 바꾸는 마지막
    /// 한 걸음이 전부 여기를 지나고, 저장·비교·전송은 계속 SystemTime만 쓴다. 기본은 브라우저
    /// 시간대이고 계정 설정이 생기면 <see cref="Override"/>만 채운다.
    /// 근거는 CLAUDE.md "시간대를 아는 곳은 DisplayTime 하나다".
    /// </summary>
    public static class DisplayTime
    {
        public static TimeZoneInfo? Override { get; set; }

        public static TimeZoneInfo Zone => Override ?? TimeZoneInfo.Local;

        /// <summary>
        /// UTC 순간 → 표시 벽시계. <c>Kind</c>는 항상 <c>Unspecified</c>로 고정한다 —
        /// <c>Local</c>·<c>Utc</c>로 표식되면 누가 <c>ToUniversalTime()</c>을 부르는 순간 오프셋이
        /// 새고, <c>DateTime</c> 비교는 <c>Kind</c>를 무시하므로 값 테스트로도 드러나지 않는다.
        /// </summary>
        public static DateTime ToWallClock(this SystemTime utc) =>
            DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc.UtcDateTime, Zone), DateTimeKind.Unspecified);

        public static string Format(this SystemTime utc, string format) =>
            utc.ToWallClock().ToString(format, CultureInfo.InvariantCulture);

        /// <summary>
        /// 픽커가 준 벽시계 입력 → UTC 순간. 입력의 <c>Kind</c>는 무시한다("화면에서 본 그 시각"이라는 뜻).
        /// 서머타임 시작일에는 그 지역에 존재하지 않는 시각이 입력될 수 있어 유효해질 때까지 민다.
        /// </summary>
        public static SystemTime FromWallClock(DateTime wallClock)
        {
            TimeZoneInfo zone = Zone;
            DateTime unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);

            while (zone.IsInvalidTime(unspecified))
            {
                unspecified = unspecified.AddMinutes(15);
            }

            return new SystemTime(TimeZoneInfo.ConvertTimeToUtc(unspecified, zone));
        }
    }
}
