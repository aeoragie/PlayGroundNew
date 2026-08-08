using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    /// <summary>공개 팀 홈페이지 탭. 라우트 슬러그는 소문자 이름 (/team/{slug}/{tab}).</summary>
    public enum SoccerTeamPublicTab
    {
        About,
        Roster,
        Record,
        Recruit,
        Career,
        Review,
    }

    public static class SoccerTeamPublicTabExtensions
    {
        public static string ToSlug(this SoccerTeamPublicTab tab)
        {
            return tab.ToString().ToLowerInvariant();
        }

        public static string ToLabel(this SoccerTeamPublicTab tab)
        {
            return tab switch
            {
                SoccerTeamPublicTab.Roster => AppText.Enums.TeamTabRoster,
                SoccerTeamPublicTab.Record => AppText.Enums.TeamTabRecord,
                SoccerTeamPublicTab.Recruit => AppText.Enums.TeamTabRecruit,
                SoccerTeamPublicTab.Career => AppText.Enums.TeamTabCareer,
                SoccerTeamPublicTab.Review => AppText.Enums.TeamTabReview,
                _ => AppText.Enums.TeamTabIntro,
            };
        }

        public static SoccerTeamPublicTab ParseSlug(string? slug)
        {
            // Enum.TryParse는 숫자 문자열("3")도 통과시키므로 이름 형태만 허용한다.
            if (!string.IsNullOrEmpty(slug)
                && !char.IsAsciiDigit(slug[0])
                && Enum.TryParse(slug, ignoreCase: true, out SoccerTeamPublicTab tab))
            {
                return tab;
            }

            return SoccerTeamPublicTab.About;
        }
    }
}
