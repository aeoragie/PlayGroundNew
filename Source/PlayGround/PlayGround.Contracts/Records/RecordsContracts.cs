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
        public int? MatchSequence { get; set; }                 // 대회 내 경기 순번 ("N경기")
        public bool HasDetail { get; set; }                     // 이벤트/출전 보유 → 행 확장 셰브론 노출
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

    /// <summary>공식 경기 상세 (Records 내 화면, 공개·읽기 전용). 대회 서비스 SingleIdx 모델 대응.
    /// 후반 스코어·주요 로그·라인업 카드 집계 등 표시 조립은 클라이언트.</summary>
    public class RecordsMatchDetailResponse
    {
        public Guid MatchId { get; set; }
        public string MatchType { get; set; } = string.Empty;   // 'Official','Friendly'
        public Guid? TournamentId { get; set; }
        public string? TournamentName { get; set; }             // 브레드크럼 (친선은 null)
        public string? Format { get; set; }                     // 'Cup','Split','League'
        public string? AgeGroup { get; set; }
        public int? SeasonYear { get; set; }
        public string? StageType { get; set; }                  // 'Group','Split1','Split2','Knockout','League'
        public string? GroupName { get; set; }
        public string? RoundName { get; set; }
        public int? MatchSequence { get; set; }                 // "N경기"
        public string Status { get; set; } = string.Empty;

        public Guid? HomeTeamId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string? HomeTeamSlug { get; set; }
        public Guid? AwayTeamId { get; set; }
        public string AwayTeamName { get; set; } = string.Empty;
        public string? AwayTeamSlug { get; set; }
        public string? HomeCoachName { get; set; }
        public string? AwayCoachName { get; set; }

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public int? HomePkScore { get; set; }
        public int? AwayPkScore { get; set; }
        public int? FirstHalfHomeScore { get; set; }            // 후반 = 총점 - 전반 (클라이언트 파생)
        public int? FirstHalfAwayScore { get; set; }

        public DateTime? MatchedAt { get; set; }
        public string? VenueName { get; set; }
        public string? RefereeName { get; set; }                // 주심
        public string? MatchTimeText { get; set; }              // 대회의 경기 시간 텍스트 ("전·후반 25분")

        public List<RecordsMatchEventDto> Events { get; set; } = new();      // 타임라인 (분 오름차순)
        public List<RecordsLineupPlayerDto> HomeLineup { get; set; } = new();
        public List<RecordsLineupPlayerDto> AwayLineup { get; set; } = new();
    }

    /// <summary>경기 이벤트 한 건 (타임라인·주요 로그). 아이콘·문구 조립은 클라이언트.</summary>
    public class RecordsMatchEventDto
    {
        public string EventType { get; set; } = string.Empty;   // 'Goal','OwnGoal','PenaltyGoal','YellowCard','RedCard'
        public int? MinuteOfPlay { get; set; }
        public string? PlayerName { get; set; }
        public Guid? PlayerId { get; set; }
        public string? PlayerSlug { get; set; }                 // Claim된 선수만 (프로필 링크)
        public int? JerseyNumber { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public bool IsHome { get; set; }                        // TeamName == HomeTeamName
    }

    /// <summary>라인업 한 선수 (선발·교체 공용). 득점 마크는 GoalMinutes로 조립.</summary>
    public class RecordsLineupPlayerDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public Guid? PlayerId { get; set; }
        public string? PlayerSlug { get; set; }                 // Claim된 선수만
        public int? JerseyNumber { get; set; }
        public string? Position { get; set; }                   // 'GK','DF','MF','FW'
        public bool IsCaptain { get; set; }
        public bool IsStarter { get; set; }
        public List<int> GoalMinutes { get; set; } = new();     // 이 선수의 득점 분 (득점 마크 "⚽N′")
    }
}
