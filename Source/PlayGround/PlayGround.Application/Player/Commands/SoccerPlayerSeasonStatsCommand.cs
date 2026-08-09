using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Player;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 시즌 통계 조회 유즈케이스 (선수 대시보드 시즌 통계 섹션). 관리 주체 본인 기준.</summary>
    public class SoccerPlayerSeasonStatsCommand
    {
        private const int MinSeasonYear = 2000;
        private const int MaxSeasonYear = 2100;

        private readonly IPlayerRepository mRepository;

        private readonly ILogger<SoccerPlayerSeasonStatsCommand> mLogger;

        public SoccerPlayerSeasonStatsCommand(IPlayerRepository repository, ILogger<SoccerPlayerSeasonStatsCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PlayerSeasonStatsResponse>> ExecuteAsync(Guid userId, int seasonYear, Guid? playerId = null, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, seasonYear, playerId, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<PlayerSeasonStatsResponse>> ExecuteCoreAsync(Guid userId, int seasonYear, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<PlayerSeasonStatsResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (seasonYear is < MinSeasonYear or > MaxSeasonYear)
            {
                return Result<PlayerSeasonStatsResponse>.Error(ErrorCode.OutOfRange, "seasonYear is out of range");
            }

            return await mRepository.GetSeasonStatsByUserAsync(userId, seasonYear, playerId, cancellation);
        }
    }
}
