namespace PlayGround.Client.Models
{
    /// <summary>경기 성격 (Design.FriendlyMatch). DB 저장 문자열과 멤버 이름이 같다.
    /// 집계(순위표·시즌 요약·공개 프로필)는 Official만 — 친선은 합산하지 않고 별도 표기한다.</summary>
    public enum SoccerMatchType
    {
        Official,
        Friendly,
    }

    /// <summary>결과 목록 세그먼트 축 (전체 / 공식 / 친선경기).</summary>
    public enum SoccerMatchSegment
    {
        All,
        Official,
        Friendly,
    }

    public static class SoccerMatchTypeExtensions
    {
        /// <summary>저장 문자열 → enum. 알 수 없으면 Official (다수가 공식이라 안전한 기본값).</summary>
        public static SoccerMatchType Parse(string? value)
        {
            return Enum.TryParse(value, out SoccerMatchType parsed) ? parsed : SoccerMatchType.Official;
        }

        public static bool IsFriendly(string? value) => Parse(value) == SoccerMatchType.Friendly;

        /// <summary>세그먼트 URL 값 — 전체는 기본값이라 쿼리에서 생략한다.</summary>
        public static string? ToQuery(this SoccerMatchSegment segment) => segment switch
        {
            SoccerMatchSegment.Official => "official",
            SoccerMatchSegment.Friendly => "friendly",
            _ => null,
        };

        public static SoccerMatchSegment ParseSegment(string? value) => value?.ToLowerInvariant() switch
        {
            "official" => SoccerMatchSegment.Official,
            "friendly" => SoccerMatchSegment.Friendly,
            _ => SoccerMatchSegment.All,
        };

        public static string ToLabel(this SoccerMatchSegment segment) => segment switch
        {
            SoccerMatchSegment.Official => "공식",
            SoccerMatchSegment.Friendly => "친선경기",
            _ => "전체",
        };

        /// <summary>세그먼트가 이 경기를 통과시키는지.</summary>
        public static bool Matches(this SoccerMatchSegment segment, string? matchType) => segment switch
        {
            SoccerMatchSegment.Official => !IsFriendly(matchType),
            SoccerMatchSegment.Friendly => IsFriendly(matchType),
            _ => true,
        };
    }
}
