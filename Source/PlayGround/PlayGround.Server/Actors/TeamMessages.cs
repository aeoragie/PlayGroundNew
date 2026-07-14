using Akka.Routing;
using PlayGround.Contracts.Team;

namespace PlayGround.Server.Actors
{
    /// <summary>팀 생성 메시지. ManagerUserId 해시로 사용자별 순차 처리(중복 제출 경합 방지).</summary>
    public sealed record CreateSoccerTeamMessage(Guid ManagerUserId, CreateTeamRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>팀 정보 묶음 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamInfoMessage(Guid ManagerUserId);

    /// <summary>선수단(로스터) 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamRosterMessage(Guid ManagerUserId);
}
