using PlayGround.Client.Localization;
using PlayGround.Domain.Soccer;

namespace PlayGround.Client.Models
{
    /// <summary>경기 목록 필터 세그먼트 (UI 전용 — 전체·공식·친선). 저장 값이 아니라 쿼리 파라미터로만 오간다.</summary>
    public enum SoccerMatchSegment
    {
        All,
        Official,
        Friendly,
    }

    public static class SoccerMatchSegmentExtensions
    {
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
            SoccerMatchSegment.Official => AppText.Enums.MatchSegmentOfficial,
            SoccerMatchSegment.Friendly => AppText.Enums.MatchSegmentFriendly,
            _ => AppText.Enums.MatchSegmentAll,
        };

        public static bool Matches(this SoccerMatchSegment segment, SoccerMatchType matchType) => segment switch
        {
            SoccerMatchSegment.Official => matchType != SoccerMatchType.Friendly,
            SoccerMatchSegment.Friendly => matchType == SoccerMatchType.Friendly,
            _ => true,
        };
    }
}
