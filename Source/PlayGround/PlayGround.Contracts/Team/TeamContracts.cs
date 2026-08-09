using PlayGround.Shared.Time;
using PlayGround.Domain.Soccer;

namespace PlayGround.Contracts.Team
{
    public class CreateTeamRequest
    {
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }
        public string? Region { get; set; }
        public List<RosterEntryDto> Roster { get; set; } = new();
    }

    public class RosterEntryDto
    {
        public string Name { get; set; } = string.Empty;
        public SoccerPosition Position { get; set; }
        public string? Number { get; set; }
    }

    public class CreateTeamResponse
    {
        public string Slug { get; set; } = string.Empty;
        public int PlayerCount { get; set; }

        public string? AccessToken { get; set; }
    }

    public class TeamInfoResponse
    {
        public TeamProfileDto Profile { get; set; } = new();
        public List<TeamValueDto> Values { get; set; } = new();
        public List<TeamCoachDto> Coaches { get; set; } = new();
        public List<TeamChannelDto> Channels { get; set; } = new();
    }

    public class TeamProfileDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }
        public string? Region { get; set; }
        public string? LogoUrl { get; set; }

        public string? CoverImageUrl { get; set; }

        public string? Description { get; set; }
        public string? Slug { get; set; }
        public bool IsVerified { get; set; }
        public int? FoundedYear { get; set; }
        public int? MonthlyFee { get; set; }
        public bool IsMonthlyFeePublic { get; set; }
        public string? TrainingDays { get; set; }
    }

    public class TeamValueDto
    {
        public Guid TeamValueId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TeamCoachDto
    {
        public Guid CoachId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Career { get; set; }
        public string? Certification { get; set; }
        public string? Quote { get; set; }
        public List<string> Achievements { get; set; } = new();
        public string? InstagramUrl { get; set; }
        public string? YoutubeUrl { get; set; }
    }

    public class TeamRosterResponse
    {
        public List<TeamRosterPlayerDto> Players { get; set; } = new();
    }

    public class AddTeamPlayerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? JerseyNumber { get; set; }
        public SoccerPosition Position { get; set; }
        public SoccerGrade Grade { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
    }

    public class TeamRosterPlayerDto
    {
        public Guid TeamPlayerId { get; set; }
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? JerseyNumber { get; set; }
        public SoccerPosition Position { get; set; }
        public SoccerGrade Grade { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? PhotoUrl { get; set; }

        public SoccerRosterClaimStatus ClaimStatus { get; set; }

        public string? InviteCode { get; set; }

        public List<string> StrengthTags { get; set; } = new();
    }

    public class TeamRecruitmentsResponse
    {
        public List<TeamRecruitmentDto> Items { get; set; } = new();
    }

    public class TeamRecruitmentDto
    {
        public Guid RecruitmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
        public SystemTime? DeadlineAt { get; set; }
        public SoccerRecruitmentStatus Status { get; set; }
        public bool IsOpen { get; set; }

        public SoccerAgeGroup AgeGroup { get; set; }

        public List<SoccerPosition> Positions { get; set; } = new();

        public int? Capacity { get; set; }

        public int AcceptedCount { get; set; }
    }

    public class SaveTeamRecruitmentRequest
    {
        public Guid RecruitmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
        public SystemTime? DeadlineAt { get; set; }

        public SoccerAgeGroup AgeGroup { get; set; }

        public List<SoccerPosition> Positions { get; set; } = new();

        public int? Capacity { get; set; }
    }

    public class ApplicationDto
    {
        public Guid ApplicationId { get; set; }
        public Guid RecruitmentId { get; set; }
        public string RecruitmentTitle { get; set; } = string.Empty;
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public SoccerAgeGroup PlayerAgeGroup { get; set; }

        public SoccerPosition PlayerPosition { get; set; }
        public string? PlayerPhotoUrl { get; set; }

        public SoccerPosition DesiredPosition { get; set; }
        public string? Introduction { get; set; }

        public SoccerApplicationStatus Status { get; set; }

        public SoccerApplicationRoute Route { get; set; }

        public string? RefAgentName { get; set; }
        public SystemTime CreatedAt { get; set; }
    }

    public class TeamApplicationsResponse
    {
        public List<ApplicationDto> Applications { get; set; } = new();
    }

    public class MyApplicationDto
    {
        public Guid ApplicationId { get; set; }

        public Guid RecruitmentId { get; set; }
        public string RecruitmentTitle { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string? TeamSlug { get; set; }

        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public SoccerPosition DesiredPosition { get; set; }

        public SoccerApplicationStatus Status { get; set; }

        public bool Confirmed { get; set; }
        public SystemTime CreatedAt { get; set; }
    }

    public class MyApplicationsResponse
    {
        public List<MyApplicationDto> Applications { get; set; } = new();
    }

    public class CreateApplicationRequest
    {
        public Guid RecruitmentId { get; set; }
        public Guid PlayerId { get; set; }
        public SoccerPosition DesiredPosition { get; set; }
        public string? Introduction { get; set; }
    }

    public class UpdateApplicationStatusRequest
    {
        public SoccerApplicationStatus Status { get; set; }
    }

    public class TeamPostsResponse
    {
        public List<TeamPostDto> Posts { get; set; } = new();
    }

    public class GuardianTeamPostsResponse
    {
        public string TeamName { get; set; } = string.Empty;
        public List<TeamPostDto> Posts { get; set; } = new();
    }

    public class TeamPostDto
    {
        public Guid PostId { get; set; }

        public SoccerTeamPostType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public bool IsPublic { get; set; }

        public string? AuthorName { get; set; }

        public SystemTime? EditedAt { get; set; }
        public SystemTime CreatedAt { get; set; }

        public int ViewCount { get; set; }

        public bool IsRead { get; set; }

        public List<TeamPostFileDto> Files { get; set; } = new();
    }

    public class TeamPostFileDto
    {
        public Guid FileId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public class SaveTeamPostRequest
    {
        public Guid PostId { get; set; }

        public SoccerTeamPostType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsPublic { get; set; }

        public List<TeamPostFileInput> Files { get; set; } = new();
    }

    public class TeamPostFileInput
    {
        public string Url { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public class TeamNewsResponse
    {
        public List<TeamNewsDto> Items { get; set; } = new();
    }

    public class TeamNewsDto
    {
        public Guid PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public SystemTime? EditedAt { get; set; }
        public SystemTime CreatedAt { get; set; }

        public List<TeamNewsFileDto> Files { get; set; } = new();
    }

    public class TeamNewsFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public class SchedulesResponse
    {
        public List<ScheduleDto> Schedules { get; set; } = new();
    }

    public class ScheduleDto
    {
        public Guid ScheduleId { get; set; }

        public SoccerScheduleType Type { get; set; }

        public string? Title { get; set; }

        public string? OpponentName { get; set; }
        public SystemTime StartsAt { get; set; }
        public string Venue { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public Guid? MatchId { get; set; }

        public bool HasResult { get; set; }
    }

    public class SaveScheduleRequest
    {
        public Guid ScheduleId { get; set; }

        public SoccerScheduleType Type { get; set; }
        public string? Title { get; set; }
        public string? OpponentName { get; set; }
        public SystemTime StartsAt { get; set; }
        public string Venue { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }

    public class TeamCareerOutcomesResponse
    {
        public List<TeamCareerOutcomeDto> Items { get; set; } = new();
    }

    public class TeamCareerOutcomeDto
    {
        public Guid OutcomeId { get; set; }
        public int OutcomeYear { get; set; }

        public SoccerCareerOutcomeType OutcomeType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public int PlayerCount { get; set; }
    }

    public class SaveTeamCareerOutcomeRequest
    {
        public Guid OutcomeId { get; set; }
        public int OutcomeYear { get; set; }
        public SoccerCareerOutcomeType OutcomeType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public int PlayerCount { get; set; } = 1;
    }

    public class DeleteTeamCareerOutcomeRequest
    {
        public Guid OutcomeId { get; set; }
        public bool Restore { get; set; }
    }

    public class TeamReviewsResponse
    {
        public List<TeamReviewDto> Items { get; set; } = new();

        public bool IsResidentGuardian { get; set; }

        public Guid? MyReviewId { get; set; }
    }

    public class TeamReviewDto
    {
        public Guid ReviewId { get; set; }
        public string AuthorDisplayName { get; set; } = string.Empty;

        public string? Meta { get; set; }
        public int Rating { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    public class SaveTeamReviewRequest
    {
        public Guid ReviewId { get; set; }
        public string TeamSlug { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    public class TeamExploreResponse
    {
        public List<TeamExploreItemDto> Teams { get; set; } = new();
    }

    public class TeamExploreItemDto
    {
        public string TeamName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? TeamType { get; set; }
        public string? Region { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsVerified { get; set; }
        public bool IsRecruiting { get; set; }

        public List<string> Values { get; set; } = new();
        public int PlayerCount { get; set; }

        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
    }

    public class TeamPublicHomeResponse
    {
        public bool IsManager { get; set; }

        public TeamPublicProfileDto Profile { get; set; } = new();
        public List<TeamValueDto> Values { get; set; } = new();
        public List<TeamCoachDto> Coaches { get; set; } = new();
        public List<TeamChannelDto> Channels { get; set; } = new();
        public List<TeamPublicPlayerDto> Roster { get; set; } = new();
    }

    public class TeamPublicProfileDto
    {
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }
        public string? Region { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? Description { get; set; }
        public string? Slug { get; set; }
        public bool IsVerified { get; set; }
        public int? FoundedYear { get; set; }
        public int? MonthlyFee { get; set; }
        public string? TrainingDays { get; set; }
    }

    public class TeamPublicPlayerDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? JerseyNumber { get; set; }
        public SoccerPosition Position { get; set; }
        public SoccerGrade Grade { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? PhotoUrl { get; set; }

        public bool HasPublicProfile { get; set; }

        public string? Slug { get; set; }

        public List<string> StrengthTags { get; set; } = new();
    }

    public class TeamMatchesResponse
    {
        public int SeasonYear { get; set; }

        public int? LeagueRank { get; set; }

        public List<TeamMatchDto> Matches { get; set; } = new();
    }

    public class TeamMatchDto
    {
        public Guid MatchId { get; set; }

        public SoccerCompetitionType CompetitionType { get; set; }

        public SoccerMatchType MatchType { get; set; }
        public string? TournamentName { get; set; }
        public SystemTime? MatchedAt { get; set; }
        public string? VenueName { get; set; }
        public bool IsHome { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public int TeamScore { get; set; }
        public int OpponentScore { get; set; }
        public List<TeamMatchEventDto> Events { get; set; } = new();
    }

    public class TeamMatchEventDto
    {
        public SoccerMatchEventType EventType { get; set; }
        public string? PlayerName { get; set; }
        public string? AssistPlayerName { get; set; }
    }

    public class TeamVideosResponse
    {
        public List<TeamVideoDto> Videos { get; set; } = new();
    }

    public class TeamVideoDto
    {
        public Guid VideoId { get; set; }
        public SoccerVideoType VideoType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int? DurationSeconds { get; set; }
        public DateOnly? RecordedOn { get; set; }
        public bool IsMatchLinked { get; set; }
    }

    public class TeamSeasonRecordResponse
    {
        public string TeamName { get; set; } = string.Empty;
        public int SeasonYear { get; set; }

        public int? LeagueRank { get; set; }

        public List<TeamMatchDto> Matches { get; set; } = new();
        public List<TeamVideoDto> Videos { get; set; } = new();
    }

    public class CreateTeamMatchResultRequest
    {
        public string OpponentName { get; set; } = string.Empty;

        public bool IsHome { get; set; } = true;

        public int OurScore { get; set; }
        public int OpponentScore { get; set; }

        public SystemTime MatchedAt { get; set; }

        public string? VenueName { get; set; }

        public List<TeamMatchScorerDto> Scorers { get; set; } = new();
    }

    public class TeamMatchScorerDto
    {
        public Guid? PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public Guid? AssistPlayerId { get; set; }
        public string? AssistPlayerName { get; set; }
        public int? MinuteOfPlay { get; set; }
    }

    public class CreateTeamMatchResultResponse
    {
        public Guid MatchId { get; set; }
    }

    public class UpdateTeamInfoRequest
    {
        public string TeamName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Region { get; set; }
        public int? FoundedYear { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public List<TeamValueInput> Values { get; set; } = new();
        public List<TeamCoachInput> Coaches { get; set; } = new();
    }

    public class TeamValueInput
    {
        public int DisplayOrder { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TeamCoachInput
    {
        public int DisplayOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Career { get; set; }
        public string? Certification { get; set; }
        public string? Quote { get; set; }

        public List<string> Achievements { get; set; } = new();
        public string? InstagramUrl { get; set; }
        public string? YoutubeUrl { get; set; }
    }

    public class UpdateTeamInfoResponse
    {
        public string? Slug { get; set; }
    }

    public class TeamTournamentOptionDto
    {
        public Guid TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public SoccerTournamentFormat Format { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
    }

    public class TeamTournamentOptionsResponse
    {
        public List<TeamTournamentOptionDto> Tournaments { get; set; } = new();
    }

    public class TeamChannelDto
    {
        public Guid ChannelId { get; set; }
        public SoccerChannelType ChannelType { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class DashboardHubResponse
    {
        public string DisplayName { get; set; } = string.Empty;

        public List<HubTeamDto> Teams { get; set; } = new();
        public List<HubChildDto> Children { get; set; } = new();

        public ActionItemsResponse Actions { get; set; } = new();

        public int ManagedCount => Teams.Count + Children.Count;
    }

    public class HubTeamDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public bool IsVerified { get; set; }
        public int PlayerCount { get; set; }

        public int PendingInviteCount { get; set; }

        public SystemTime? NextMatchStartsAt { get; set; }

        public string? NextMatchOpponent { get; set; }
    }

    public class HubChildDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? Slug { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? TeamName { get; set; }
        public SoccerPosition Position { get; set; }
        public string? JerseyNumber { get; set; }

        public int Appearances { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }

        public SoccerRosterClaimStatus ClaimStatus { get; set; } = SoccerRosterClaimStatus.Claimed;

        public int CorrectionPendingCount { get; set; }

        public int ApplicationPendingCount { get; set; }

        public bool ApplicationActionNeeded { get; set; }

        public int TeamNewsUnreadCount { get; set; }

        public SystemTime? RequestedAt { get; set; }
    }

    public class ActionItemsResponse
    {
        public int TotalCount { get; set; }

        public List<ActionItemDto> Items { get; set; } = new();
    }

    public class ActionItemDto
    {
        public string Kind { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid? TeamId { get; set; }
        public Guid? MatchId { get; set; }

        public SystemTime OccurredAt { get; set; }
    }

    public class PendingInvitesResponse
    {
        public List<PendingInviteDto> Invites { get; set; } = new();
    }

    public class PendingInviteDto
    {
        public Guid InviteId { get; set; }
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public Guid? PlayerId { get; set; }

        public string? PlayerName { get; set; }
        public SystemTime CreatedAt { get; set; }
    }

    public class CreateRecordCorrectionRequest
    {
        public Guid MatchId { get; set; }

        public SoccerCorrectionField FieldType { get; set; }

        public string? CurrentValue { get; set; }

        public string RequestedValue { get; set; } = string.Empty;
        public string? Description { get; set; }

        public Guid? TargetPlayerId { get; set; }
    }

    public class RecordCorrectionsResponse
    {
        public List<RecordCorrectionDto> Corrections { get; set; } = new();
    }

    public class RecordCorrectionDto
    {
        public Guid CorrectionId { get; set; }
        public Guid MatchId { get; set; }
        public SoccerCorrectionField FieldType { get; set; }
        public string? CurrentValue { get; set; }
        public string RequestedValue { get; set; } = string.Empty;
        public string? Description { get; set; }

        public SoccerCorrectionStatus Status { get; set; }

        public string? RejectReason { get; set; }

        public SystemTime RequestedAt { get; set; }
        public SystemTime? ReviewedAt { get; set; }

        public string? TournamentName { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public SystemTime? MatchedAt { get; set; }
    }
}
