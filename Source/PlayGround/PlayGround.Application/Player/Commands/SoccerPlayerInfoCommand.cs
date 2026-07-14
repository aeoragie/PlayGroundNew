using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 프로필 묶음 조회 유즈케이스 (선수 대시보드 프로필 섹션). 관리 주체 본인 기준.</summary>
    public class SoccerPlayerInfoCommand
    {
        private readonly IPlayerRepository mRepository;

        public SoccerPlayerInfoCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<PlayerInfoResponse>> ExecuteAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<PlayerInfoResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            Result<PlayerInfoResponse?> info = await mRepository.GetInfoByUserAsync(userId, cancellation);
            if (info.IsError)
            {
                return Result<PlayerInfoResponse>.Failure(info.ResultData);
            }

            if (info.Value is null)
            {
                return Result<PlayerInfoResponse>.Error(ErrorCode.NotFound, "player not found for user");
            }

            return Result<PlayerInfoResponse>.Success(info.Value);
        }
    }
}
