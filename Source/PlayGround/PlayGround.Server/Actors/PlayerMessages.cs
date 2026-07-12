using Akka.Routing;
using PlayGround.Contracts.Player;

namespace PlayGround.Server.Actors
{
    /// <summary>선수 프로필 생성 메시지. UserId 해시로 사용자별 순차 처리(중복 제출 경합 방지).</summary>
    public sealed record CreatePlayerProfileMessage(Guid UserId, CreatePlayerProfileRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }
}
