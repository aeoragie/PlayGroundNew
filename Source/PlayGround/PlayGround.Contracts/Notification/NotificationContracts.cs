using System;
using System.Collections.Generic;

namespace PlayGround.Contracts.Notification
{
    /// <summary>알림 묶음 — 미읽음 카운트(벨 뱃지, 목록 컷과 무관한 전체 수) + 최근 목록.</summary>
    public class NotificationsResponse
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Items { get; set; } = new();
    }

    /// <summary>알림 한 건 — 표시 문구·딥링크는 클라이언트가 Type + 스냅샷으로 조립한다.</summary>
    public class NotificationDto
    {
        public Guid NotificationId { get; set; }

        /// <summary>SoccerNotificationType 멤버 이름 문자열.</summary>
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
        public DateTime CreatedAt { get; set; }

        /// <summary>액션형(ClaimRequest) 전용 — 요청의 현재 상태(라이브). 'Pending'이면 승인/거절 버튼.</summary>
        public string? RequestStatus { get; set; }
    }
}
