using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 커리어 목록 조회 유즈케이스 (선수 대시보드 커리어 섹션). 관리 주체 본인 기준.</summary>
    public class SoccerPlayerCareerCommand
    {
        private readonly IPlayerRepository mRepository;

        public SoccerPlayerCareerCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<PlayerCareerResponse>> ExecuteAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<PlayerCareerResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            return await mRepository.GetCareersByUserAsync(userId, cancellation);
        }
    }
}
