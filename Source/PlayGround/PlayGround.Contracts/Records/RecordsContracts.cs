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

    /// <summary>대회 상세 묶음 (Records 상세 화면, 공개). 통계 바·형식별 탭 구성은 클라이언트.</summary>
    public class RecordsTournamentDetailResponse
    {
        public RecordsTournamentDetailDto Tournament { get; set; } = new();
        public List<RecordsStandingDto> Standings { get; set; } = new();
        public List<RecordsMatchDto> Matches { get; set; } = new();
        public List<RecordsAwardDto> Awards { get; set; } = new();
        public List<RecordsSeriesChampionDto> SeriesChampions { get; set; } = new();
        public List<RecordsVideoDto> Videos { get; set; } = new();
        public List<RecordsNewsDto> News { get; set; } = new();
    }

    /// <summary>대회 상세 기본 정보 (히어로 + 개요 카드).</summary>
    public class RecordsTournamentDetailDto
    {
        public Guid TournamentId { get; set; }
        public int SeasonYear { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string AgeGroup { get; set; } = string.Empty;
        public string? RegionGroup { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? TeamCount { get; set; }
        public string? HostName { get; set; }
        public string? MethodText { get; set; }
        public string? MatchTimeText { get; set; }
        public string? VenueText { get; set; }
        public string? TiebreakText { get; set; }
        public string? RegulationPdfUrl { get; set; }
        public string? SourceName { get; set; }
    }

    /// <summary>순위표 한 행. 키 = (StageType, GroupName). 득실차는 클라이언트 파생.</summary>
    public class RecordsStandingDto
    {
        public string StageType { get; set; } = string.Empty;   // 'Group','Split1','Split2','League'
        public string? GroupName { get; set; }                  // '1조'… (리그·스플릿은 null)
        public Guid? TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamSlug { get; set; }                   // 공개 팀 홈 링크 (없으면 텍스트)
        public int TeamRank { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int Points { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public bool IsQualified { get; set; }
    }

    /// <summary>경기 한 건. PK 스코어는 괄호 표기용 ("1 (4)") — 표시 조립은 클라이언트.</summary>
    public class RecordsMatchDto
    {
        public Guid MatchId { get; set; }
        public string? StageType { get; set; }
        public string? GroupName { get; set; }
        public string? RoundName { get; set; }                  // 조별 'R1'~, 토너먼트 'PO','R16','QF','SF','F'
        public Guid? HomeTeamId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string? HomeTeamSlug { get; set; }
        public Guid? AwayTeamId { get; set; }
        public string AwayTeamName { get; set; } = string.Empty;
        public string? AwayTeamSlug { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public int? HomePkScore { get; set; }
        public int? AwayPkScore { get; set; }
        public string Status { get; set; } = string.Empty;      // 'Scheduled','Completed','Canceled'
        public DateTime? MatchedAt { get; set; }
        public string? VenueName { get; set; }
    }

    /// <summary>수상 한 건.</summary>
    public class RecordsAwardDto
    {
        public string AwardType { get; set; } = string.Empty;   // 'Champion','RunnerUp','FairPlay'
        public Guid? TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamSlug { get; set; }
    }

    /// <summary>역대 우승 한 건 (같은 SeriesSlug의 타 연도 Champion).</summary>
    public class RecordsSeriesChampionDto
    {
        public int SeasonYear { get; set; }
        public Guid? TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamSlug { get; set; }
    }

    /// <summary>경기 영상 한 건. VS 배너 팀명은 연결된 경기에서 채운다 (없으면 null).</summary>
    public class RecordsVideoDto
    {
        public Guid VideoId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string VideoType { get; set; } = string.Empty;   // 'Highlight','FullMatch','Training'
        public int? DurationSeconds { get; set; }
        public DateOnly? RecordedOn { get; set; }
        public string? HomeTeamName { get; set; }
        public string? AwayTeamName { get; set; }
        public string? VenueName { get; set; }
    }

    /// <summary>대회 뉴스 한 건.</summary>
    public class RecordsNewsDto
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? PublisherName { get; set; }
        public DateOnly? PublishedOn { get; set; }
    }
}
