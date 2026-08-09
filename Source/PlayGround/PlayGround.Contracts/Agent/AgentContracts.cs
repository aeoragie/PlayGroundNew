using PlayGround.Shared.Time;
using PlayGround.Domain.Soccer;

namespace PlayGround.Contracts.Agent
{
    public class AgentViewRequestResponse
    {
        public Guid RequestId { get; set; }

        public SoccerAgentRequestStatus Status { get; set; }

        public string Message { get; set; } = string.Empty;
        public SystemTime RequestedAt { get; set; }
        public SystemTime? ExpiresAt { get; set; }

        public bool IsExpired { get; set; }

        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public SoccerAgeGroup PlayerAgeGroup { get; set; }
        public SoccerPosition PlayerPosition { get; set; }

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
        public SoccerAgentViewEvent EventType { get; set; }
        public SystemTime CreatedAt { get; set; }
    }

    public class ReviewAgentViewRequestRequest
    {
        public Guid RequestId { get; set; }
        public SoccerAgentReviewAction Action { get; set; }
    }

    public class AgentRequestEligibilityResponse
    {
        public SoccerAgentEligibility Status { get; set; }

        public SystemTime? CooldownUntil { get; set; }

        public bool CanRequest => Status == SoccerAgentEligibility.Allowed;
    }
}
