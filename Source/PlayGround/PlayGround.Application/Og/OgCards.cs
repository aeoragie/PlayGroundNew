namespace PlayGround.Application.Og
{
    /// <summary>팀 OG 카드 원자료 (DECISION.OGMETA). 공개 팀만 — 비공개·미존재는 조회가 null.</summary>
    public class TeamOgCard
    {
        public string TeamName { get; set; } = string.Empty;
        public string? Region { get; set; }
        public string? AgeGroup { get; set; }
        public int PlayerCount { get; set; }

        /// <summary>엠블럼 경로(/uploads/...) — 없으면 이니셜 실드로 렌더.</summary>
        public string? LogoUrl { get; set; }
    }

    public class TournamentOgCard
    {
        public string Name { get; set; } = string.Empty;
        public string? AgeGroup { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int TeamCount { get; set; }
    }
}
