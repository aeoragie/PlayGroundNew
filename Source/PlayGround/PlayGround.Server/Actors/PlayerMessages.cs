using Akka.Routing;
using PlayGround.Contracts.Player;

namespace PlayGround.Server.Actors
{
    /// <summary>선수 프로필 생성 메시지. UserId로 일관 해싱 → 같은 사용자의 요청은 한 라우티에 순차 처리
    /// (중복 제출 등 자기 자신에 대한 경합 방지 = 쓰기에서 액터가 값어치 있는 지점).</summary>
    public sealed record CreatePlayerProfileMessage(Guid UserId, CreatePlayerProfileRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }
}
