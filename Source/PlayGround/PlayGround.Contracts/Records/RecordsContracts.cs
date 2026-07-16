using System;
using System.Collections.Generic;

namespace PlayGround.Contracts.Records
{
    /// <summary>시즌 대회/리그 목록 (Records 목록·아카이브 공용, 공개).</summary>
    public class RecordsTournamentsResponse
    {
        public int SeasonYear { get; set; }

        /// <summary>기록이 있는 연도 목록 (내림차순) — 아카이브 연도 칩.</summary>
        public List<int> SeasonYears { get; set; } = new();

        public List<RecordsTournamentDto> Tournaments { get; set; } = new();
    }

    /// <summary>대회/리그 한 건 (목록 행). 정렬·그룹핑·표시 라벨은 클라이언트.</summary>
    public class RecordsTournamentDto
    {
        public Guid TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;      // SoccerTournamentFormat 멤버 이름 ('Cup','Split','League')
        public string Scope { get; set; } = string.Empty;       // SoccerTournamentScope 멤버 이름 ('National','Regional')
        public string AgeGroup { get; set; } = string.Empty;    // 'U12','U15','U18'
        public string? RegionGroup { get; set; }                // 리그 지역 그룹·개최지 ('서울')
        public string Status { get; set; } = string.Empty;      // SoccerTournamentStatus 멤버 이름 ('Scheduled','InProgress','Completed')
        public int? TeamCount { get; set; }
        public string? ChampionTeamName { get; set; }           // 아카이브 '우승' 뱃지 (Champion 수상 팀)
    }
}
