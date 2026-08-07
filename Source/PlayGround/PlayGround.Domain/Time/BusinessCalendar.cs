using System.Collections.Concurrent;
using PlayGround.Shared.Time;

namespace PlayGround.Domain.Time
{
    /// <summary>
    /// **그 데이터가 속한 지역의 업무 달력.** 저장·비교는 전부 UTC(<see cref="SystemTime"/>)로 하고,
    /// "그 지역 달력으로 며칠인가"를 판정할 때만 여기를 지난다.
    ///
    /// 그런 값은 두 종류다.
    /// - **마감일** — 사용자가 고른 "8/10 마감"은 그 지역 날짜의 끝까지다
    /// - **시즌 연도** — "올해 전적"의 올해는 그 지역 달력의 해다
    ///
    /// 그 외(일정·경기 시각·감사 컬럼)는 전부 순간이라 UTC로 저장하고 표시만 되돌린다.
    /// 여기를 거치지 않는다.
    ///
    /// <para>
    /// **보는 사람의 시간대가 아니다.** 서울 팀이 올린 "8/10 마감"은 방콕에서 봐도 서울 8/10이다.
    /// 화면 표시는 `PlayGround.Client`의 `DisplayTime`이 따로 맡는다 — 축이 다르다.
    /// </para>
    ///
    /// <para>
    /// **지역이 늘어도 클래스를 늘리지 않는다.** 새 나라는 코드가 아니라 **데이터**(그 팀·대회의
    /// 시간대 id)로 는다. `JapanTime` 같은 클래스를 또 만들면 오프셋을 아는 곳이 둘이 되고
    /// 거기서부터 어긋난다.
    /// </para>
    ///
    /// <para>
    /// **고정 오프셋을 쓰지 않는다.** 전 세계를 대상으로 하면 서머타임이 있는 지역
    /// (미국·유럽·남미·중동 일부)이 들어오고, 그 규칙은 해마다·정치적으로 바뀐다.
    /// 그래서 IANA 시간대 데이터베이스(`Asia/Seoul`·`America/New_York`)를 쓴다 —
    /// 나라 단위로는 부족하다(미국·인도네시아는 한 나라에 여러 존이 있다).
    /// </para>
    ///
    /// 원시 `DateTime`은 이 파일 내부 계산에만 존재한다(`TimeBaselineGuardTests` 허용 목록).
    /// </summary>
    public static class BusinessCalendar
    {
        /// <summary>기본 지역 — 데이터에 시간대가 없을 때의 임시 기준(<see cref="Unresolved"/>).</summary>
        private const string DefaultZoneId = "Asia/Seoul";

        /// <summary>id → 존 캐시. `FindSystemTimeZoneById`는 매번 조회하면 비싸다.</summary>
        private static readonly ConcurrentDictionary<string, TimeZoneInfo> Zones = new();

        /// <summary>
        /// 아직 데이터가 지역을 모를 때 쓰는 임시 기준.
        ///
        /// **팀·대회에 시간대 컬럼이 생기면 이 멤버를 지운다** — 지우는 순간 컴파일러가
        /// 남은 호출부를 전부 짚어 준다. 그게 이걸 기본값 매개변수로 두지 않은 이유다
        /// (기본값이면 조용히 넘어가서 영영 안 고쳐진다).
        /// </summary>
        public static TimeZoneInfo Unresolved => Resolve(DefaultZoneId);

        /// <summary>
        /// IANA 시간대 id를 존으로. .NET 6+는 Windows에서도 IANA id를 받는다.
        /// 알 수 없는 id는 <see cref="TimeZoneNotFoundException"/> — 조용히 UTC로 떨어뜨리지 않는다
        /// (그러면 마감이 그 지역과 몇 시간씩 어긋난 채로 돌아간다).
        /// </summary>
        public static TimeZoneInfo Resolve(string ianaZoneId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ianaZoneId);
            return Zones.GetOrAdd(ianaZoneId, TimeZoneInfo.FindSystemTimeZoneById);
        }

        /// <summary>오늘 (그 지역의 달력).</summary>
        public static DateOnly Today(TimeZoneInfo zone) => LocalDateOf(SystemTime.Now, zone);

        /// <summary>올해 (그 지역의 달력). 시즌 연도 계산용.</summary>
        public static int CurrentYear(TimeZoneInfo zone) => Today(zone).Year;

        /// <summary>UTC 순간 → 그 순간이 속한 지역 날짜. 마감일을 폼에 되돌릴 때 쓴다.</summary>
        public static DateOnly LocalDateOf(SystemTime utc, TimeZoneInfo zone)
        {
            ArgumentNullException.ThrowIfNull(zone);
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc.UtcDateTime, zone));
        }

        /// <summary>UTC 순간 → 지역 달력 기준 연도. "그 경기는 몇 년도 시즌인가" 판정용.</summary>
        public static int YearOf(SystemTime utc, TimeZoneInfo zone) => LocalDateOf(utc, zone).Year;

        /// <summary>
        /// 지역 날짜의 **끝**을 UTC 순간으로. 마감일 저장에 쓴다 —
        /// "8/10 마감"은 그 지역의 8/10이 끝날 때까지 유효하므로, DB에는 그 순간을 UTC로 넣고
        /// 프로시저는 `[DeadlineAt] > GETUTCDATE()` 하나로 판정한다(시간대가 SQL에 안 들어간다).
        ///
        /// **다음 날 0시에서 1틱을 뺀다.** 로컬에서 "23:59:59.999"를 만들어 변환하지 않는 이유는,
        /// 서머타임 전환이 자정 근처에 걸리면 그 시각이 아예 존재하지 않을 수 있어서다.
        /// UTC로 옮긴 뒤 빼면 전환과 무관하게 항상 "그 날의 마지막 순간"이 된다.
        /// </summary>
        public static SystemTime EndOfDayToUtc(DateOnly localDate, TimeZoneInfo zone) =>
            StartOfDayToUtc(localDate.AddDays(1), zone).AddTicks(-1);

        /// <summary>지역 날짜의 시작(0시)을 UTC 순간으로.</summary>
        public static SystemTime StartOfDayToUtc(DateOnly localDate, TimeZoneInfo zone) =>
            ToUtc(localDate.ToDateTime(TimeOnly.MinValue), zone);

        /// <summary>
        /// 지역 달력 기준 한 해의 [시작, 끝) 을 UTC 순간으로. 시즌 집계 쿼리에 넘긴다 —
        /// **SQL이 시간대 산술을 하지 않게** 하려는 것이고, 범위 비교라 인덱스도 탄다
        /// (`YEAR(DATEADD(...))`은 인덱스를 못 쓴다).
        /// </summary>
        public static (SystemTime StartUtc, SystemTime EndUtc) YearRangeUtc(int year, TimeZoneInfo zone) =>
            (StartOfDayToUtc(new DateOnly(year, 1, 1), zone),
             StartOfDayToUtc(new DateOnly(year + 1, 1, 1), zone));

        /// <summary>
        /// 지역 벽시계 → UTC 순간. 이 파일 밖에서는 벽시계 `DateTime`을 만들지 않는다.
        ///
        /// 서머타임 전환에 걸리는 두 경우를 여기서 흡수한다.
        /// - **존재하지 않는 시각**(봄, 시계가 건너뛴 구간) — 유효해질 때까지 앞으로 민다
        /// - **두 번 있는 시각**(가을, 시계가 되돌아간 구간) — .NET이 표준시로 해석한다(먼저 온 쪽)
        /// </summary>
        private static SystemTime ToUtc(DateTime localWallClock, TimeZoneInfo zone)
        {
            ArgumentNullException.ThrowIfNull(zone);

            DateTime unspecified = DateTime.SpecifyKind(localWallClock, DateTimeKind.Unspecified);

            // 전환 폭은 지역마다 다르다(보통 1시간, Lord Howe는 30분) — 폭을 가정하지 않고 조금씩 민다
            while (zone.IsInvalidTime(unspecified))
            {
                unspecified = unspecified.AddMinutes(15);
            }

            return new SystemTime(TimeZoneInfo.ConvertTimeToUtc(unspecified, zone));
        }
    }
}
