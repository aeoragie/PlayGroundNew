using PlayGround.Shared.Time;
using PlayGround.Contracts.Soccer;

namespace PlayGround.Contracts.Agent
{
    public class AgentViewRequestResponse
    {
        public Guid RequestId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public SystemTime RequestedAt { get; set; }
        public SystemTime? ExpiresAt { get; set; }

        public bool IsExpired { get; set; }

        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public SoccerAgeGroup? PlayerAgeGroup { get; set; }
        public SoccerPosition? PlayerPosition { get; set; }

        public AgentProfileDto Agent { get; set; } = new();
        public List<AgentViewLogDto> Logs { get; set; } = new();
    }

    public class AgentProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string? AgencyName { get; set; }
        public int? RegisteredYear { get; set; }
        public bool IsVerified { get; set; }
        public int BrokerageCount { get; set; }
        public decimal? Rating { get; set; }
        public string? ActiveRegions { get; set; }
    }

    public class AgentViewLogDto
    {
        public string EventType { get; set; } = string.Empty;
        public SystemTime CreatedAt { get; set; }
    }

    public class ReviewAgentViewRequestRequest
    {
        public Guid RequestId { get; set; }
        public string Action { get; set; } = string.Empty;
    }

    public class AgentRequestEligibilityResponse
    {
        public string Status { get; set; } = string.Empty;

        public SystemTime? CooldownUntil { get; set; }

        public bool CanRequest => Status == "Allowed";
    }
}
