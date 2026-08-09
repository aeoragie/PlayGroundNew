using PlayGround.Shared.Time;

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

        public string Type { get; set; } = string.Empty;

        public Guid RefId { get; set; }
        public Guid? TargetPlayerId { get; set; }

        public string? ActorName { get; set; }
        public string? PlayerName { get; set; }
        public string? TeamName { get; set; }
        public string? MetaText { get; set; }
        public string? SubText { get; set; }
        public string? Relation { get; set; }

        public bool IsRead { get; set; }
        public SystemTime CreatedAt { get; set; }

        public string? RequestStatus { get; set; }
    }
}
