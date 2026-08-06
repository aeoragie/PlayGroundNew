using PlayGround.Shared.Time;

namespace PlayGround.Domain.Time
{
    /// <summary>
    /// **+9시간을 아는 유일한 곳.** 저장·비교는 전부 UTC(<see cref="SystemTime"/>)로 하고,
    /// 여기는 "한국 달력"이 기준인 값에만 쓴다.
    ///
    /// 그런 값은 두 종류뿐이다.
    /// - **마감일** — 사용자가 고른 "8/10 마감"은 한국 날짜의 끝(23:59:59.999 KST)까지다
    /// - **시즌 연도** — "올해 전적"의 올해는 한국 달력의 해다
    ///
    /// 그 외(일정·경기 시각·감사 컬럼)는 전부 순간이라 UTC로 저장하고
    /// 표시만 브라우저 시간대로 되돌린다. 여기를 거치지 않는다.
    ///
    /// 달력 개념은 <see cref="DateOnly"/>, 순간은 <see cref="SystemTime"/>으로만 주고받는다.
    /// 원시 `DateTime`은 이 파일 내부 계산에만 존재한다(`TimeBaselineGuardTests` 허용 목록).
    ///
    /// 고정 오프셋으로 계산한다. **한국은 1988년 이후 서머타임이 없어 연중 UTC+9**라
    /// 시간대 데이터베이스(Windows `Korea Standard Time` / IANA `Asia/Seoul`)가 필요 없다.
    /// </summary>
    public static class KoreanTime
    {
        /// <summary>KST의 UTC 오프셋. 서머타임이 없어 연중 고정이다.</summary>
        public static readonly TimeSpan Offset = TimeSpan.FromHours(9);

        /// <summary>오늘(한국 달력).</summary>
        public static DateOnly Today => ToKoreanDate(SystemTime.Now);

        /// <summary>올해(한국 달력). 시즌 연도 계산용.</summary>
        public static int CurrentYear => Today.Year;

        /// <summary>UTC 순간 → 그 순간이 속한 한국 날짜. 마감일을 폼에 되돌릴 때 쓴다.</summary>
        public static DateOnly ToKoreanDate(SystemTime utc) =>
            DateOnly.FromDateTime(utc.UtcDateTime + Offset);

        /// <summary>UTC 순간 → 한국 벽시계 기준 연도. "그 경기는 몇 년도 시즌인가" 판정용.</summary>
        public static int KoreanYearOf(SystemTime utc) => ToKoreanDate(utc).Year;

        /// <summary>
        /// 한국 날짜의 **끝**을 UTC 순간으로. 마감일 저장에 쓴다 —
        /// "8/10 마감"은 8/10 23:59:59.999 KST까지 유효하므로, DB에는 그 순간을 UTC로 넣고
        /// 프로시저는 `[DeadlineAt] > GETUTCDATE()` 하나로 판정한다(9시간 상수가 SQL에 안 들어간다).
        /// </summary>
        public static SystemTime EndOfDayToUtc(DateOnly koreanDate) =>
            ToUtc(koreanDate.ToDateTime(TimeOnly.MinValue).AddDays(1).AddTicks(-1));

        /// <summary>
        /// 한국 달력 기준 한 해의 [시작, 끝) 을 UTC 순간으로. 시즌 집계 쿼리에 넘긴다 —
        /// **SQL이 시간대 산술을 하지 않게** 하려는 것이고, 범위 비교라 인덱스도 탄다
        /// (`YEAR(DATEADD(...))`은 인덱스를 못 쓴다).
        /// </summary>
        public static (SystemTime StartUtc, SystemTime EndUtc) YearRangeUtc(int koreanYear) =>
            (ToUtc(new DateTime(koreanYear, 1, 1)), ToUtc(new DateTime(koreanYear + 1, 1, 1)));

        /// <summary>한국 벽시계 → UTC 순간. 이 파일 밖에서는 벽시계 `DateTime`을 만들지 않는다.</summary>
        private static SystemTime ToUtc(DateTime koreanWallClock) =>
            new(DateTime.SpecifyKind(koreanWallClock - Offset, DateTimeKind.Utc));
    }
}
