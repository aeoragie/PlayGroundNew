using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 시즌 통계 조회 유즈케이스 (선수 대시보드 시즌 통계 섹션). 관리 주체 본인 기준.</summary>
    public class SoccerPlayerSeasonStatsCommand
    {
        private const int MinSeasonYear = 2000;
        private const int MaxSeasonYear = 2100;

        private readonly IPlayerRepository mRepository;

        public SoccerPlayerSeasonStatsCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<PlayerSeasonStatsResponse>> ExecuteAsync(Guid userId, int seasonYear, Guid? playerId = null, CancellationToken cancellation = default)
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
