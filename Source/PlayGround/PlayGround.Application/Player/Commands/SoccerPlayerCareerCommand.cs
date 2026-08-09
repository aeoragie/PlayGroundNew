using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Player;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 커리어 목록 조회 유즈케이스 (선수 대시보드 커리어 섹션). 관리 주체 본인 기준.</summary>
    public class SoccerPlayerCareerCommand
    {
        private readonly IPlayerRepository mRepository;
        private readonly ILogger<SoccerPlayerCareerCommand> mLogger;

        public SoccerPlayerCareerCommand(IPlayerRepository repository, ILogger<SoccerPlayerCareerCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PlayerCareerResponse>> ExecuteAsync(Guid userId, Guid? playerId = null, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, playerId, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<PlayerCareerResponse>> ExecuteCoreAsync(Guid userId, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<PlayerCareerResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            return await mRepository.GetCareersByUserAsync(userId, playerId, cancellation);
        }
    }
}
