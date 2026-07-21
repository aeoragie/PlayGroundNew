using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 탐색 공개 목록 조회 유즈케이스 (비로그인). 0건은 빈 목록 — 에러가 아니다.</summary>
    public class SoccerTeamExploreCommand
    {
        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamExploreCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<TeamExploreResponse>> ExecuteAsync(CancellationToken cancellation = default)
        {
            return await mRepository.GetExploreTeamsAsync(cancellation);
        }
    }
}
