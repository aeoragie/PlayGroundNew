using PlayGround.Shared.Time;
using PlayGround.Domain.Soccer;

namespace PlayGround.Contracts.Player
{
    public class CreatePlayerProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? BirthDate { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? Region { get; set; }
    }

    public class CreatePlayerProfileResponse
    {
        public Guid PlayerId { get; set; }

        public string? AccessToken { get; set; }
    }

    public class ManagedPlayersResponse
    {
        public List<ManagedPlayerDto> Players { get; set; } = new();
    }

    public class ManagedPlayerDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? Slug { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? PhotoUrl { get; set; }
        public string? TeamName { get; set; }
        public string? JerseyNumber { get; set; }
        public SoccerPosition Position { get; set; }
        public bool IsGuardianManaged { get; set; }
    }

    public class PlayerInfoResponse
    {
        public PlayerProfileDto Profile { get; set; } = new();

        public List<PlayerFieldVisibilityDto> Visibilities { get; set; } = new();
        public List<PlayerFamilyMemberDto> Family { get; set; } = new();
    }

    public class PlayerProfileDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? Slug { get; set; }
        public string? PhotoUrl { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public int? BirthYear { get; set; }
        public SoccerGrade Grade { get; set; }
        public SoccerPosition Position { get; set; }
        public string? JerseyNumber { get; set; }
        public string? TeamName { get; set; }
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public SoccerPreferredFoot PreferredFoot { get; set; }
        public string? SchoolName { get; set; }
        public string? GuardianPhoneMasked { get; set; }
        public bool IsGuardianManaged { get; set; }

        public bool CanEditPhoto { get; set; }

        public List<string> StrengthTags { get; set; } = new();
    }

    public class PlayerFieldVisibilityDto
    {
        public SoccerPlayerProfileField FieldName { get; set; }
        public bool IsPublic { get; set; }
    }

    public class PlayerFamilyMemberDto
    {
        public string MemberName { get; set; } = string.Empty;
        public SoccerFamilyRole Role { get; set; }
        public bool HasAccount { get; set; }
    }

    public class SetPlayerFieldVisibilityRequest
    {
        public SoccerPlayerProfileField FieldName { get; set; }
        public bool IsPublic { get; set; }
    }

    public class UpdatePlayerProfileInfoRequest
    {
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public SoccerPreferredFoot PreferredFoot { get; set; }
        public string? SchoolName { get; set; }

        public string? Slug { get; set; }
    }

    public class UpdatePlayerProfileInfoResponse
    {
        public bool SlugTaken { get; set; }
    }

    public class SetPlayerPhotoRequest
    {
        public Guid PlayerId { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class ClaimPlayerInviteRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    public class ClaimPlayerInviteResponse
    {
        public string PlayerName { get; set; } = string.Empty;
        public string? TeamName { get; set; }

        public string? AccessToken { get; set; }
    }

    public class PlayerSeasonStatsResponse
    {
        public int SeasonYear { get; set; }

        public List<int> SeasonYears { get; set; } = new();

        public List<PlayerMatchStatDto> Matches { get; set; } = new();
    }

    public class PlayerMatchStatDto
    {
        public Guid MatchId { get; set; }
        public SystemTime? MatchedAt { get; set; }

        public SoccerCompetitionType CompetitionType { get; set; }

        public SoccerMatchType MatchType { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public int TeamScore { get; set; }
        public int OpponentScore { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int? MinutesPlayed { get; set; }
    }

    public class PlayerCareerResponse
    {
        public List<PlayerCareerEntryDto> Entries { get; set; } = new();
    }

    public class PlayerCareerEntryDto
    {
        public Guid CareerId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public string? BadgeLabel { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Role { get; set; }
        public string? Note { get; set; }
        public bool IsVerified { get; set; }
    }

    public class SavePlayerCareerRequest
    {
        public Guid CareerId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Role { get; set; }
        public string? Note { get; set; }
        public string? BadgeLabel { get; set; }
    }

    public class DeletePlayerCareerRequest
    {
        public Guid CareerId { get; set; }
        public bool Restore { get; set; }
    }

    public class SavePlayerPortfolioVideoRequest
    {
        public Guid VideoId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateOnly? RecordedOn { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class DeletePlayerPortfolioVideoRequest
    {
        public Guid VideoId { get; set; }
        public bool Restore { get; set; }
    }

    public class PlayerPortfolioResponse
    {
        public List<PlayerPortfolioVideoDto> Videos { get; set; } = new();
    }

    public class PlayerPortfolioVideoDto
    {
        public Guid VideoId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int? DurationSeconds { get; set; }
        public bool IsPrimary { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateOnly? RecordedOn { get; set; }
    }

    public class PlayerPublicProfileResponse
    {
        public PlayerPublicHeaderDto Profile { get; set; } = new();

        public PlayerPublicSeasonDto? Season { get; set; }

        public PlayerPortfolioVideoDto? PrimaryVideo { get; set; }

        public int VideoCount { get; set; }

        public List<PlayerCareerEntryDto> Careers { get; set; } = new();

        public PlayerPublicGrantDto? Grant { get; set; }

        public List<PlayerMatchStatDto>? Matches { get; set; }
    }

    public class PlayerPublicGrantDto
    {
        public SystemTime ApprovedAt { get; set; }
        public SystemTime ExpiresAt { get; set; }
    }

    public class PlayerPublicHeaderDto
    {
        public string Name { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public bool IsGuardianManaged { get; set; }
        public SoccerPosition Position { get; set; }
        public string? JerseyNumber { get; set; }
        public int? BirthYear { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string? TeamName { get; set; }
        public string? TeamSlug { get; set; }
        public bool TeamIsVerified { get; set; }

        public bool IsClaimable { get; set; }
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public SoccerPreferredFoot PreferredFoot { get; set; }

        public string? SchoolName { get; set; }

        public SoccerGrade Grade { get; set; }

        public string? GuardianDisplayName { get; set; }

        public List<string> StrengthTags { get; set; } = new();
    }

    public class PlayerPublicSeasonDto
    {
        public int SeasonYear { get; set; }
        public int MatchCount { get; set; }
        public int TotalMinutes { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int? AverageMinutes { get; set; }
    }

    public class StrengthTagPresetsResponse
    {
        public List<StrengthTagPresetDto> Presets { get; set; } = new();
    }

    public class StrengthTagPresetDto
    {
        public SoccerPosition Position { get; set; }
        public string Tag { get; set; } = string.Empty;
    }

    public class SaveStrengthTagsRequest
    {
        public List<string> Tags { get; set; } = new();
    }
}
