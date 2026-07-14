using Akka.Routing;
using PlayGround.Contracts.Player;

namespace PlayGround.Server.Actors
{
    /// <summary>선수 프로필 생성 메시지. UserId 해시로 사용자별 순차 처리(중복 제출 경합 방지).</summary>
    public sealed record CreatePlayerProfileMessage(Guid UserId, CreatePlayerProfileRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>선수 프로필 묶음 조회 메시지 (같은 사용자 쓰기와 순차 처리 — UserId 해시).</summary>
    public sealed record GetSoccerPlayerInfoMessage(Guid UserId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>항목 공개 설정 변경 메시지 (쓰기 — UserId 해시로 사용자별 순차).</summary>
    public sealed record SetSoccerPlayerFieldVisibilityMessage(Guid UserId, SetPlayerFieldVisibilityRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>초대코드 Claim 메시지 (쓰기 — UserId 해시). CurrentRole은 JWT 클레임 — General만 승격.</summary>
    public sealed record ClaimSoccerPlayerInviteMessage(Guid UserId, string? CurrentRole, ClaimPlayerInviteRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }
}
