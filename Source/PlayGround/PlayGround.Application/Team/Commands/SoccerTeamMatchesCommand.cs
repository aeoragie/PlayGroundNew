using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 시즌 경기 결과 조회 유즈케이스 (팀 대시보드 경기 결과 섹션). 관리자 본인 팀 기준.</summary>
    public class SoccerTeamMatchesCommand
    {
        private const int MinSeasonYear = 2000;
        private const int MaxSeasonYear = 2100;

        private readonly ISoccerTeamRepository mRepository;

        private readonly ILogger<SoccerTeamMatchesCommand> mLogger;

        public SoccerTeamMatchesCommand(ISoccerTeamRepository repository, ILogger<SoccerTeamMatchesCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<TeamMatchesResponse>> ExecuteAsync(Guid managerUserId, int seasonYear, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(managerUserId, seasonYear, cancellation)).LogWith(mLogger, "Execute", ("ManagerUserId", managerUserId));

        private async Task<Result<TeamMatchesResponse>> ExecuteCoreAsync(Guid managerUserId, int seasonYear, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamMatchesResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            if (seasonYear is < MinSeasonYear or > MaxSeasonYear)
            {
                return Result<TeamMatchesResponse>.Error(ErrorCode.OutOfRange, "seasonYear is out of range");
            }

            return await mRepository.GetTeamMatchesByManagerAsync(managerUserId, seasonYear, cancellation);
        }
    }
}
