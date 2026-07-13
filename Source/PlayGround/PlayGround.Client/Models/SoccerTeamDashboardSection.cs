namespace PlayGround.Client.Models
{
    /// <summary>축구 팀 대시보드 섹션. 라우트 슬러그는 소문자 이름 (/dashboard/team/{slug}).</summary>
    public enum SoccerTeamDashboardSection
    {
        Info,
        Roster,
        Schedule,
        Results,
        Videos,
        Recruit,
    }

    public static class SoccerTeamDashboardSectionExtensions
    {
        /// <summary>라우트 슬러그로 변환 (예: Results → "results").</summary>
        public static string ToSlug(this SoccerTeamDashboardSection section)
        {
            return section.ToString().ToLowerInvariant();
        }

        /// <summary>라우트 슬러그를 섹션으로 해석. 미지정·미지원 슬러그는 Info.</summary>
        public static SoccerTeamDashboardSection ParseSlug(string? slug)
        {
            // Enum.TryParse는 숫자 문자열("3")도 통과시키므로 이름 형태만 허용한다.
            if (!string.IsNullOrEmpty(slug)
                && !char.IsAsciiDigit(slug[0])
                && Enum.TryParse(slug, ignoreCase: true, out SoccerTeamDashboardSection section))
            {
                return section;
            }

            return SoccerTeamDashboardSection.Info;
        }
    }
}
