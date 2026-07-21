namespace PlayGround.Domain.Soccer
{
    /// <summary>진학·진로 사례 유형 (Design.TeamPublicHome ⑤). 멤버 이름 = DB 저장 문자열 (SoccerTeamCareerOutcomes.OutcomeType).</summary>
    public enum SoccerCareerOutcomeType
    {
        /// <summary>프로 산하 이적.</summary>
        ProTransfer,

        /// <summary>축구부 진학.</summary>
        SchoolTeam,

        /// <summary>상급 연령팀 승격.</summary>
        Promotion,
    }

    public static class SoccerCareerOutcomeTypeExtensions
    {
        /// <summary>타임라인 유형 태그 라벨 (dc 원문).</summary>
        public static string ToTagLabel(this SoccerCareerOutcomeType type)
        {
            return type switch
            {
                SoccerCareerOutcomeType.ProTransfer => "프로 산하",
                SoccerCareerOutcomeType.SchoolTeam => "축구부",
                _ => "승격",
            };
        }

        /// <summary>요약 카드 라벨 (dc 원문 — 모바일 "상급팀 승격" 축약은 화면 분기).</summary>
        public static string ToSummaryLabel(this SoccerCareerOutcomeType type)
        {
            return type switch
            {
                SoccerCareerOutcomeType.ProTransfer => "프로 산하 이적",
                SoccerCareerOutcomeType.SchoolTeam => "축구부 진학",
                _ => "상급 연령팀 승격",
            };
        }
    }
}
