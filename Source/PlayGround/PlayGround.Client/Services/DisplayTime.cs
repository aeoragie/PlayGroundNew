using System.Globalization;
using PlayGround.Domain.Time;
using PlayGround.Shared.Time;

namespace PlayGround.Client.Services
{
    /// <summary>
    /// **표시 시간대의 단일 결정권자.** SystemTime(UTC)을 사용자에게 보여줄 벽시계로 바꾸는
    /// 마지막 한 걸음이 전부 여기를 지난다. 저장·비교·전송은 여전히 SystemTime만 쓴다.
    ///
    /// 지금은 **한국 시간(UTC+9) 고정**이다 (2026-08-06 결정 — 사용자층이 국내라
    /// 브라우저 시간대와 사실상 같다). 계정 시간대 설정이 생기면 이 클래스 안의
    /// 오프셋 결정만 바뀌고 호출부는 그대로다.
    ///
    /// SystemTime 자체에는 표시 변환이 없다 — 시간대를 아는 곳을 하나로 좁히기 위해
    /// 일부러 여기로 분리했다(`TimeBaselineGuardTests`가 `ToLocalTime()` 사용을 막는다).
    /// </summary>
    public static class DisplayTime
    {
        /// <summary>UTC 순간 → 표시 벽시계(지금은 KST). 캘린더 그리드·월 그룹핑 같은 달력 산술용.</summary>
        public static DateTime ToWallClock(this SystemTime utc) => utc.UtcDateTime + KoreanTime.Offset;

        /// <summary>표시 문자열 — 화면에 보이는 시각은 이걸로 만든다.</summary>
        public static string Format(this SystemTime utc, string format) =>
            utc.ToWallClock().ToString(format, CultureInfo.InvariantCulture);

        /// <summary>픽커가 준 벽시계 입력(지금은 KST 해석) → UTC 순간.</summary>
        public static SystemTime FromWallClock(DateTime wallClock) =>
            new(DateTime.SpecifyKind(wallClock - KoreanTime.Offset, DateTimeKind.Utc));
    }
}
