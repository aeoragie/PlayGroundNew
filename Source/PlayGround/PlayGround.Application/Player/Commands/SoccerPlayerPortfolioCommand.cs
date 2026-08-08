using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 포트폴리오 영상 목록 조회 유즈케이스 (선수 대시보드 포트폴리오 섹션). 관리 주체 본인 기준.</summary>
    public class SoccerPlayerPortfolioCommand
    {
        private readonly IPlayerRepository mRepository;
        private readonly ILogger<SoccerPlayerPortfolioCommand> mLogger;

        public SoccerPlayerPortfolioCommand(IPlayerRepository repository, ILogger<SoccerPlayerPortfolioCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PlayerPortfolioResponse>> ExecuteAsync(Guid userId, Guid? playerId = null, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, playerId, cancellation)).LogWith(mLogger, "Execute");

        private async Task<Result<PlayerPortfolioResponse>> ExecuteCoreAsync(Guid userId, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<PlayerPortfolioResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            return await mRepository.GetPortfolioByUserAsync(userId, playerId, cancellation);
        }
    }
}
