using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Settings;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Settings.Commands
{
    /// <summary>계정 설정 조회 유즈케이스 (설정 · 계정 탭). 이메일은 마스킹된 값만 내려간다.</summary>
    public class AccountSettingsCommand
    {
        private readonly IAccountRepository mRepository;
        private readonly ILogger<AccountSettingsCommand> mLogger;

        public AccountSettingsCommand(IAccountRepository repository, ILogger<AccountSettingsCommand> logger)
        {
            Debug.Assert(repository != null);
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AccountSettingsResponse>> ExecuteAsync(Guid userId, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<AccountSettingsResponse>> ExecuteCoreAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<AccountSettingsResponse>.Error(ErrorCode.InvalidInput, "userId required");
            }

            Result<AccountSettingsResponse?> settings = await mRepository.GetSettingsAsync(userId, cancellation);
            if (settings.IsError)
            {
                return Result<AccountSettingsResponse>.Failure(settings.ResultData);
            }

            if (settings.Value is null)
            {
                return Result<AccountSettingsResponse>.Error(ErrorCode.NotFound, "user not found");
            }

            return Result<AccountSettingsResponse>.Success(settings.Value);
        }
    }
}
