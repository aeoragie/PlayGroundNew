using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Team.Models;

namespace PlayGround.Application.Interfaces
{
    /// <summary>팀 저장·조회 포트 (Persistence에서 구현). 생성 시 최종 슬러그 반환.</summary>
    public interface ISoccerTeamRepository
    {
        Task<Result<string>> CreateWithRosterAsync(CreateTeamInput input, CancellationToken cancellation = default);

        /// <summary>관리자 기준 팀 정보 묶음 조회. 팀 미존재 시 Success(null) — 에러가 아니다.</summary>
        Task<Result<TeamInfoResponse?>> GetTeamInfoByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>관리자 기준 선수단(로스터) 조회. 팀 미존재·로스터 없음은 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamRosterResponse>> GetTeamRosterByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);
    }
}
