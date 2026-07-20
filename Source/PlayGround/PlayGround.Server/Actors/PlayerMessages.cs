using Akka.Routing;
using PlayGround.Contracts.Player;

namespace PlayGround.Server.Actors
{
    /// <summary>선수 프로필 생성 메시지. UserId 해시로 사용자별 순차 처리(중복 제출 경합 방지).</summary>
    public sealed record CreatePlayerProfileMessage(Guid UserId, CreatePlayerProfileRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>관리 중인 자녀 목록 조회 메시지 (읽기 — 같은 사용자 쓰기와 순차 처리).</summary>
    public sealed record GetSoccerManagedPlayersMessage(Guid UserId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>선수 프로필 묶음 조회 메시지 (같은 사용자 쓰기와 순차 처리 — UserId 해시).</summary>
    public sealed record GetSoccerPlayerInfoMessage(Guid UserId, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>항목 공개 설정 변경 메시지 (쓰기 — UserId 해시로 사용자별 순차).</summary>
    public sealed record SetSoccerPlayerFieldVisibilityMessage(Guid UserId, SetPlayerFieldVisibilityRequest Data, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>선수 사진 설정·삭제 메시지 (쓰기 — UserId 해시). 대상 PlayerId는 본인이 아닐 수 있다(팀 관리자 경로).</summary>
    public sealed record SetSoccerPlayerPhotoMessage(Guid UserId, SetPlayerPhotoRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>시즌 통계 조회 메시지 (같은 사용자 쓰기와 순차 처리 — UserId 해시).</summary>
    public sealed record GetSoccerPlayerSeasonStatsMessage(Guid UserId, int SeasonYear, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>초대코드 Claim 메시지 (쓰기 — UserId 해시). CurrentRole은 JWT 클레임 — General만 승격.</summary>
    public sealed record ClaimSoccerPlayerInviteMessage(Guid UserId, string? CurrentRole, ClaimPlayerInviteRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>커리어 이력 저장 메시지 (쓰기 — UserId 해시로 사용자별 순차).</summary>
    public sealed record SaveSoccerPlayerCareerMessage(Guid UserId, SavePlayerCareerRequest Data, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>커리어 이력 삭제·복구 메시지 (쓰기 — UserId 해시).</summary>
    public sealed record DeleteSoccerPlayerCareerMessage(Guid UserId, DeletePlayerCareerRequest Data, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>포트폴리오 영상 저장 메시지 (쓰기 — UserId 해시. 대표 지정이 다른 행을 건드려 순차성이 중요하다).</summary>
    public sealed record SaveSoccerPlayerPortfolioVideoMessage(Guid UserId, SavePlayerPortfolioVideoRequest Data, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>포트폴리오 영상 삭제·복구 메시지 (쓰기 — UserId 해시).</summary>
    public sealed record DeleteSoccerPlayerPortfolioVideoMessage(Guid UserId, DeletePlayerPortfolioVideoRequest Data, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>커리어 목록 조회 메시지 (같은 사용자 쓰기와 순차 처리 — UserId 해시).</summary>
    public sealed record GetSoccerPlayerCareerMessage(Guid UserId, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>포트폴리오 영상 목록 조회 메시지 (같은 사용자 쓰기와 순차 처리 — UserId 해시).</summary>
    public sealed record GetSoccerPlayerPortfolioMessage(Guid UserId, Guid? PlayerId) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }
}
