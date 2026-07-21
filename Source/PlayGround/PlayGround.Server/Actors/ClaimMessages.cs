using Akka.Routing;
using PlayGround.Contracts.Claim;

namespace PlayGround.Server.Actors
{
    /// <summary>초대코드 선수 카드 조회 메시지 (읽기 — 사용자 스코프가 없어 UserId 해시로 분산만).</summary>
    public sealed record GetClaimInviteCardMessage(Guid UserId, string Code) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>연결 요청 생성 메시지 (쓰기 — UserId 해시로 사용자별 순차, 이중 제출 경합 방지).</summary>
    public sealed record CreateClaimRequestMessage(Guid UserId, string RequesterName, CreateClaimRequestRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>내 연결 요청 조회 메시지 (재방문 복원 — 같은 사용자 쓰기와 순차).</summary>
    public sealed record GetOwnClaimRequestMessage(Guid UserId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>연결 요청 승인/거절 메시지 (쓰기 — ManagerUserId 해시로 관리자별 순차).</summary>
    public sealed record ReviewClaimRequestMessage(Guid ManagerUserId, ReviewClaimRequestRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>알림 목록 조회 메시지 (조회 시점 지연 생성이 있어 같은 사용자와 순차 처리).</summary>
    public sealed record GetNotificationsMessage(Guid UserId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>알림 읽음 처리 메시지 (쓰기 — UserId 해시).</summary>
    public sealed record MarkNotificationReadMessage(Guid UserId, Guid NotificationId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }
}
