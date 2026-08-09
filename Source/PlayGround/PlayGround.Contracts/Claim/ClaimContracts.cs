using PlayGround.Shared.Time;
using PlayGround.Domain.Soccer;

namespace PlayGround.Contracts.Claim
{
    public class ClaimInviteCardResponse
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public SoccerPosition Position { get; set; }
        public string? JerseyNumber { get; set; }
        public int? BirthYear { get; set; }
        public SoccerAgeGroup AgeGroup { get; set; }
        public string TeamName { get; set; } = string.Empty;
    }

    public class CreateClaimRequestRequest
    {
        public string Code { get; set; } = string.Empty;
        public SoccerClaimRelation Relation { get; set; }
    }

    public class CreateClaimByPlayerRequest
    {
        public Guid PlayerId { get; set; }
        public SoccerClaimRelation Relation { get; set; }
    }

    public class PendingChildClaimDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public SoccerAgeGroup AgeGroup { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public SystemTime RequestedAt { get; set; }
    }

    public class ClaimRequestSummaryResponse
    {
        public Guid RequestId { get; set; }
        public SoccerClaimRequestStatus Status { get; set; }
        public SoccerClaimRelation Relation { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public SystemTime RequestedAt { get; set; }
    }

    public class ReviewClaimRequestRequest
    {
        public Guid RequestId { get; set; }
        public bool Approve { get; set; }
    }

    public class ReviewClaimResponse
    {
        public Guid RequestId { get; set; }
        public SoccerClaimRequestStatus Status { get; set; }
        public string PlayerName { get; set; } = string.Empty;
    }
}
