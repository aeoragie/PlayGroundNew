using PlayGround.Shared.Time;
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

    /// <summary>알림 센터 페이지(/notifications) 한 페이지 — 세그먼트 카운트 3종 + 현재 필터의 항목.
    /// hasMore는 클라가 offset+Items.Count &lt; 필터별 카운트로 파생(무한 스크롤 금지 · 더 보기 20).</summary>
    public class NotificationPageResponse
    {
        public List<NotificationDto> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int ActionRequiredCount { get; set; }

        /// <summary>읽지 않음 세그먼트 수 (벨 카운트와 동일 기준).</summary>
        public int UnreadCount { get; set; }
    }

    /// <summary>여러 건 읽음 처리 요청 (페이지 진입 시 화면에 보인 알림 Id 목록).</summary>
    public class MarkNotificationsReadRequest
    {
        public List<Guid> NotificationIds { get; set; } = new();
    }

    /// <summary>알림 한 건 — 표시 문구·딥링크는 클라이언트가 Type + 스냅샷으로 조립한다.</summary>
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

        /// <summary>액션형(ClaimRequest) 전용 — 요청의 현재 상태(라이브). 'Pending'이면 승인/거절 버튼.</summary>
        public string? RequestStatus { get; set; }
    }
}
