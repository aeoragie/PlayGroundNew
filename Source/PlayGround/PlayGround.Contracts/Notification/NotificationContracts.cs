using PlayGround.Shared.Time;
using PlayGround.Domain.Soccer;

namespace PlayGround.Contracts.Notification
{
    public class NotificationsResponse
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Items { get; set; } = new();
    }

    public class NotificationPageResponse
    {
        public List<NotificationDto> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int ActionRequiredCount { get; set; }

        public int UnreadCount { get; set; }
    }

    public class MarkNotificationsReadRequest
    {
        public List<Guid> NotificationIds { get; set; } = new();
    }

    public class NotificationDto
    {
        public Guid NotificationId { get; set; }

        public SoccerNotificationType Type { get; set; }

        public Guid RefId { get; set; }
        public Guid? TargetPlayerId { get; set; }

        public string? ActorName { get; set; }
        public string? PlayerName { get; set; }
        public string? TeamName { get; set; }
        public string? MetaText { get; set; }
        public string? SubText { get; set; }
        public SoccerClaimRelation Relation { get; set; }

        public bool IsRead { get; set; }
        public SystemTime CreatedAt { get; set; }

        public SoccerClaimRequestStatus RequestStatus { get; set; }
    }
}
