using PlayGround.Shared.Time;
using PlayGround.Contracts.Soccer;

namespace PlayGround.Contracts.Records
{
    public class RecordsTournamentsResponse
    {
        public int SeasonYear { get; set; }

        public List<int> SeasonYears { get; set; } = new();

        public List<RecordsTournamentDto> Tournaments { get; set; } = new();
    }

    public class RecordsTournamentDto
    {
        public Guid TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public SoccerTournamentFormat Format { get; set; }
        public SoccerTournamentScope Scope { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? RegionGroup { get; set; }
        public SoccerTournamentStatus Status { get; set; }
        public int? TeamCount { get; set; }
        public string? ChampionTeamName { get; set; }
    }

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

    public class RecordsTournamentDetailDto
    {
        public Guid TournamentId { get; set; }
        public int SeasonYear { get; set; }
        public string Name { get; set; } = string.Empty;
        public SoccerTournamentFormat Format { get; set; }
        public SoccerTournamentScope Scope { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? RegionGroup { get; set; }
        public SoccerTournamentStatus Status { get; set; }
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

    public class RecordsStandingDto
    {
        public SoccerStageType StageType { get; set; }
        public string? GroupName { get; set; }
        public Guid? TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamSlug { get; set; }
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

    public class RecordsMatchDto
    {
        public Guid MatchId { get; set; }
        public SoccerStageType StageType { get; set; }
        public string? GroupName { get; set; }
        public string? RoundName { get; set; }
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
        public SoccerMatchStatus Status { get; set; }
        public SystemTime? MatchedAt { get; set; }
        public string? VenueName { get; set; }
        public int? MatchSequence { get; set; }
        public bool HasDetail { get; set; }
    }

    public class RecordsAwardDto
    {
        public SoccerAwardType AwardType { get; set; }
        public Guid? TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamSlug { get; set; }
    }

    public class RecordsSeriesChampionDto
    {
        public int SeasonYear { get; set; }
        public Guid? TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamSlug { get; set; }
    }

    public class RecordsVideoDto
    {
        public Guid VideoId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public SoccerVideoType VideoType { get; set; }
        public int? DurationSeconds { get; set; }
        public DateOnly? RecordedOn { get; set; }
        public string? HomeTeamName { get; set; }
        public string? AwayTeamName { get; set; }
        public string? VenueName { get; set; }
    }

    public class RecordsNewsDto
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? PublisherName { get; set; }
        public DateOnly? PublishedOn { get; set; }
    }

    public class RecordsMatchDetailResponse
    {
        public Guid MatchId { get; set; }
        public SoccerMatchType MatchType { get; set; }
        public Guid? TournamentId { get; set; }
        public string? TournamentName { get; set; }
        public SoccerTournamentFormat Format { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public int? SeasonYear { get; set; }
        public SoccerStageType StageType { get; set; }
        public string? GroupName { get; set; }
        public string? RoundName { get; set; }
        public int? MatchSequence { get; set; }
        public SoccerMatchStatus Status { get; set; }

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
        public int? FirstHalfHomeScore { get; set; }
        public int? FirstHalfAwayScore { get; set; }

        public SystemTime? MatchedAt { get; set; }
        public string? VenueName { get; set; }
        public string? RefereeName { get; set; }
        public string? MatchTimeText { get; set; }

        public List<RecordsMatchEventDto> Events { get; set; } = new();
        public List<RecordsLineupPlayerDto> HomeLineup { get; set; } = new();
        public List<RecordsLineupPlayerDto> AwayLineup { get; set; } = new();
    }

    public class RecordsMatchEventDto
    {
        public SoccerMatchEventType EventType { get; set; }
        public int? MinuteOfPlay { get; set; }
        public string? PlayerName { get; set; }
        public Guid? PlayerId { get; set; }
        public string? PlayerSlug { get; set; }
        public int? JerseyNumber { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public bool IsHome { get; set; }
    }

    public class RecordsLineupPlayerDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public Guid? PlayerId { get; set; }
        public string? PlayerSlug { get; set; }
        public int? JerseyNumber { get; set; }
        public SoccerPosition Position { get; set; }
        public bool IsCaptain { get; set; }
        public bool IsStarter { get; set; }
        public List<int> GoalMinutes { get; set; } = new();
    }
}
