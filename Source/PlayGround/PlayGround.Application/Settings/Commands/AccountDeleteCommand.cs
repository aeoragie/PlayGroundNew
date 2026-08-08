using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Settings.Commands
{
    /// <summary>계정 삭제 유즈케이스 — 소프트 삭제(DeletedAt 마킹). 로그인·조회가 전부 차단된다.
    /// 자녀 프로필 이전·동반 삭제는 후속 플로우(핸드오프 캡션 명시)로, 여기서는 계정만 마킹한다.</summary>
    public class AccountDeleteCommand
    {
        private readonly IAccountRepository mRepository;
        private readonly ILogger<AccountDeleteCommand> mLogger;

        public AccountDeleteCommand(IAccountRepository repository, ILogger<AccountDeleteCommand> logger)
        {
            Debug.Assert(repository != null);
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> ExecuteAsync(Guid userId, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, cancellation)).LogWith(mLogger, "Execute");

        private async Task<Result<bool>> ExecuteCoreAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "userId required");
            }

            Result<bool> deleted = await mRepository.SoftDeleteAsync(userId, cancellation);
            if (deleted.IsError)
            {
                return deleted;
            }

            if (!deleted.Value)
            {
                return Result<bool>.Error(ErrorCode.NotFound, "user not found or already deleted");
            }

            return Result<bool>.Success(true);
        }
    }
}
