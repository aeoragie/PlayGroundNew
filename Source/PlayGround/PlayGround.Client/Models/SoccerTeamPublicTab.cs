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
        /// <summary>라우트 슬러그로 변환 (예: Record → "record").</summary>
        public static string ToSlug(this SoccerTeamPublicTab tab)
        {
            return tab.ToString().ToLowerInvariant();
        }

        /// <summary>탭 라벨 (SPEC.TEAMPUBLICHOME.md 카피 고정).</summary>
        public static string ToLabel(this SoccerTeamPublicTab tab)
        {
            return tab switch
            {
                SoccerTeamPublicTab.Roster => "선수단",
                SoccerTeamPublicTab.Record => "시즌성적",
                SoccerTeamPublicTab.Recruit => "모집",
                SoccerTeamPublicTab.Career => "진학·진로",
                SoccerTeamPublicTab.Review => "리뷰",
                _ => "소개",
            };
        }

        /// <summary>라우트 슬러그를 탭으로 해석. 미지정·미지원 슬러그는 About.</summary>
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
