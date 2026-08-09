using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Team;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 탐색 공개 목록 조회 유즈케이스 (비로그인). 0건은 빈 목록 — 에러가 아니다.</summary>
    public class SoccerTeamExploreCommand
    {
        private readonly ISoccerTeamRepository mRepository;
        private readonly ILogger<SoccerTeamExploreCommand> mLogger;

        public SoccerTeamExploreCommand(ISoccerTeamRepository repository, ILogger<SoccerTeamExploreCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<TeamExploreResponse>> ExecuteAsync(CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(cancellation)).LogWith(mLogger, "Execute");

        private async Task<Result<TeamExploreResponse>> ExecuteCoreAsync(CancellationToken cancellation = default)
        {
            return await mRepository.GetExploreTeamsAsync(cancellation);
        }
    }
}
