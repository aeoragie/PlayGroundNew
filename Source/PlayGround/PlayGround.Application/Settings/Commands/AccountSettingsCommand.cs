using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Settings;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Settings.Commands
{
    /// <summary>계정 설정 조회 유즈케이스 (설정 · 계정 탭). 이메일은 마스킹된 값만 내려간다.</summary>
    public class AccountSettingsCommand
    {
        private readonly IAccountRepository mRepository;

        public AccountSettingsCommand(IAccountRepository repository)
        {
            Debug.Assert(repository != null);
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<AccountSettingsResponse>> ExecuteAsync(Guid userId, CancellationToken cancellation = default)
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
