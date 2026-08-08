using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>
    /// 이 계정이 관리하는 선수(자녀) 목록 조회 유즈케이스.
    /// **보호자는 자녀가 여러 명일 수 있다** — 선수 대시보드의 자녀 전환과 대시보드 허브가
    /// 어느 자녀를 보여줄지 정하는 근거가 이 목록이다.
    /// </summary>
    public class SoccerManagedPlayersCommand
    {
        private readonly IPlayerRepository mRepository;
        private readonly ILogger<SoccerManagedPlayersCommand> mLogger;

        public SoccerManagedPlayersCommand(IPlayerRepository repository, ILogger<SoccerManagedPlayersCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ManagedPlayersResponse>> ExecuteAsync(Guid userId, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<ManagedPlayersResponse>> ExecuteCoreAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<ManagedPlayersResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            return await mRepository.GetManagedPlayersAsync(userId, cancellation);
        }
    }
}
