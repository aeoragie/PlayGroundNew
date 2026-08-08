using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    /// <summary>축구 선수 대시보드 섹션. 라우트 슬러그는 소문자 이름 (/dashboard/player/{slug}).</summary>
    public enum SoccerPlayerDashboardSection
    {
        Profile,
        Career,
        Stats,
        Portfolio,
    }

    public static class SoccerPlayerDashboardSectionExtensions
    {
        public static string ToSlug(this SoccerPlayerDashboardSection section)
        {
            return section.ToString().ToLowerInvariant();
        }

        /// <summary>메뉴·탭 라벨 (SPEC.PLAYERDASHBOARD.md 카피 고정).</summary>
        public static string ToLabel(this SoccerPlayerDashboardSection section)
        {
            return section switch
            {
                SoccerPlayerDashboardSection.Career => AppText.Enums.PlayerSectionCareer,
                SoccerPlayerDashboardSection.Stats => AppText.Enums.PlayerSectionStats,
                SoccerPlayerDashboardSection.Portfolio => AppText.Enums.PlayerSectionPortfolio,
                _ => AppText.Enums.PlayerSectionProfile,
            };
        }

        public static SoccerPlayerDashboardSection ParseSlug(string? slug)
        {
            // Enum.TryParse는 숫자 문자열("3")도 통과시키므로 이름 형태만 허용한다.
            if (!string.IsNullOrEmpty(slug)
                && !char.IsAsciiDigit(slug[0])
                && Enum.TryParse(slug, ignoreCase: true, out SoccerPlayerDashboardSection section))
            {
                return section;
            }

            return SoccerPlayerDashboardSection.Profile;
        }
    }
}
