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

    /// <summary>공개 팀 홈페이지 조회 메시지 (비로그인, Slug 기준 — RoundRobin).</summary>
    public sealed record GetSoccerTeamHomeMessage(string Slug);

    /// <summary>시즌 경기 결과 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamMatchesMessage(Guid ManagerUserId, int SeasonYear);

    /// <summary>경기영상 목록 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamVideosMessage(Guid ManagerUserId);

    /// <summary>공개 팀 홈 시즌성적 조회 메시지 (비로그인, Slug 기준 — RoundRobin).</summary>
    public sealed record GetSoccerTeamSeasonRecordMessage(string Slug, int SeasonYear);

    /// <summary>팀 정보 수정 메시지. ManagerUserId 해시로 순차 처리 — 가치·코치 통째 교체 경합 방지.</summary>
    public sealed record UpdateSoccerTeamInfoMessage(Guid ManagerUserId, UpdateTeamInfoRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>결과 입력 폼의 대회 선택지 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTournamentOptionsMessage(Guid ManagerUserId, int SeasonYear);

    /// <summary>경기 결과 저장 메시지. ManagerUserId 해시로 순차 처리 — 같은 팀의 중복 제출·순위표 재계산 경합을 막는다.</summary>
    public sealed record CreateSoccerMatchResultMessage(Guid ManagerUserId, CreateTeamMatchResultRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }
}
