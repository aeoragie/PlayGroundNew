using PlayGround.Shared.Time;

namespace PlayGround.Domain.Time
{
    /// <summary>
    /// **+9시간을 아는 유일한 곳.** 저장·비교는 전부 UTC(<see cref="SystemTime"/>)로 하고,
    /// 여기는 "한국 달력"이 기준인 값에만 쓴다.
    ///
    /// 그런 값은 두 종류뿐이다:
    /// - **마감일** — 사용자가 고른 "8/10 마감"은 한국 날짜의 끝(23:59:59.999 KST)까지다
    /// - **시즌 연도** — "올해 전적"의 올해는 한국 달력의 해다
    ///
    /// 그 외(일정·경기 시각·감사 컬럼)는 전부 순간이라 UTC로 저장하고
    /// 표시만 브라우저 시간대로 되돌린다 — 여기를 거치지 않는다.
    ///
    /// 고정 오프셋으로 계산한다. **한국은 1988년 이후 서머타임이 없어 연중 UTC+9**라
    /// 시간대 데이터베이스(Windows `Korea Standard Time` / IANA `Asia/Seoul`)가 필요 없다.
    /// </summary>
    public static class KoreanTime
    {
        /// <summary>KST의 UTC 오프셋. 서머타임이 없어 연중 고정이다.</summary>
        public static readonly TimeSpan Offset = TimeSpan.FromHours(9);

        /// <summary>지금의 한국 벽시계 시각. **비교에 쓰지 말 것** — 비교는 UTC끼리 한다.</summary>
        public static DateTime Now => SystemTime.Now + Offset;

        /// <summary>오늘(한국 달력) 자정.</summary>
        public static DateTime Today => Now.Date;

        /// <summary>올해(한국 달력). 시즌 연도 계산용.</summary>
        public static int CurrentYear => Now.Year;

        /// <summary>한국 벽시계 → UTC.</summary>
        public static DateTime ToUtc(DateTime koreanWallClock) =>
            DateTime.SpecifyKind(koreanWallClock - Offset, DateTimeKind.Utc);

        /// <summary>UTC → 한국 벽시계.</summary>
        public static DateTime ToKorean(DateTime utc) => utc + Offset;

        /// <summary>
        /// 한국 날짜의 **끝**을 UTC 순간으로. 마감일 저장에 쓴다 —
        /// "8/10 마감"은 8/10 23:59:59.999 KST까지 유효하므로, DB에는 그 순간을 UTC로 넣고
        /// 프로시저는 `[DeadlineAt] > GETUTCDATE()` 하나로 판정한다(9시간 상수가 SQL에 안 들어간다).
        /// </summary>
        public static DateTime EndOfDayToUtc(DateTime koreanDate) =>
            ToUtc(koreanDate.Date.AddDays(1).AddTicks(-1));

        /// <summary>UTC 순간 → 그 순간이 속한 한국 날짜. 마감일을 폼에 되돌릴 때 쓴다.</summary>
        public static DateTime ToKoreanDate(DateTime utc) => ToKorean(utc).Date;

        /// <summary>
        /// 한국 달력 기준 한 해의 [시작, 끝) 을 UTC 순간으로. 시즌 집계 쿼리에 넘긴다 —
        /// **SQL이 시간대 산술을 하지 않게** 하려는 것이고, 범위 비교라 인덱스도 탄다
        /// (`YEAR(DATEADD(...))`은 인덱스를 못 쓴다).
        /// </summary>
        public static (DateTime StartUtc, DateTime EndUtc) YearRangeUtc(int koreanYear) =>
            (ToUtc(new DateTime(koreanYear, 1, 1)), ToUtc(new DateTime(koreanYear + 1, 1, 1)));
    }
}
