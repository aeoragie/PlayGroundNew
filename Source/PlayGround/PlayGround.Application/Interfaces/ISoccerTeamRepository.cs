using PlayGround.Shared.Result;
using PlayGround.Application.Team.Models;

namespace PlayGround.Application.Interfaces
{
    /// <summary>팀+로스터 저장 포트 (Persistence에서 구현). 생성 시 최종 슬러그 반환.</summary>
    public interface ISoccerTeamRepository
    {
        Task<Result<string>> CreateWithRosterAsync(CreateTeamInput input, CancellationToken cancellation = default);
    }
}
