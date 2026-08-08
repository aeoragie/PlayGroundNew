using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Auth;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Settings.Commands
{
    /// <summary>이름(DisplayName) 변경 유즈케이스 (Design.SettingsFlows ①). 검증 → 저장(30일 2회 제한은 SP가 원자 판정) →
    /// **JWT 재발급**(새 name 클레임)으로 GNB·프로필 즉시 반영(역할 승격 재발급과 같은 패턴).
    /// 동명이인 허용(중복 검사 없음). 성공 시 새 토큰을 담은 AuthResult 반환.</summary>
    public class DisplayNameChangeCommand
    {
        private readonly IAccountRepository mRepository;
        private readonly IJwtTokenService mTokenService;
        private readonly ILogger<DisplayNameChangeCommand> mLogger;

        public DisplayNameChangeCommand(IAccountRepository repository, IJwtTokenService tokenService, ILogger<DisplayNameChangeCommand> logger)
        {
            Debug.Assert(repository != null && tokenService != null);
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mTokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AuthResult>> ExecuteAsync(Guid userId, string displayName, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, displayName, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<AuthResult>> ExecuteCoreAsync(Guid userId, string displayName, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<AuthResult>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            string name = (displayName ?? string.Empty).Trim();
            if (!IsValidName(name))
            {
                return Result<AuthResult>.Error(ErrorCode.InvalidInput, "invalid display name");
            }

            Result<AccountUser?> updated = await mRepository.UpdateDisplayNameAsync(userId, name, cancellation);
            if (updated.IsError)
            {
                return Result<AuthResult>.Failure(updated.ResultData);
            }

            mLogger.InfoWith("Display name updated", ("UserId", userId));

            // 빈 결과 = 제한 초과·미변경·미존재 (SP 원자 판정). 클라가 제한을 미리 막지만 서버가 최종 방어.
            if (updated.Value is null)
            {
                return Result<AuthResult>.Error(ErrorCode.InvalidInput, "name change not applied (limit or unchanged)");
            }

            AccountUser user = updated.Value;
            string accessToken = mTokenService.GenerateAccessToken(
                user.UserId, user.Email, user.DisplayName, user.UserRole, user.ProfileImageUrl);

            return Result<AuthResult>.Success(new AuthResult
            {
                AccessToken = accessToken,
                User = new AuthUserDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Role = user.UserRole,
                    ProfileImageUrl = user.ProfileImageUrl
                }
            });
        }

        /// <summary>한글 2~10자(전부 한글) 또는 영문 2~20자(전부 영문). 특수문자·숫자·공백·혼용 불가.
        /// 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다.</summary>
        private static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            bool allHangul = name.All(c => c >= '가' && c <= '힣');
            if (allHangul)
            {
                return name.Length is >= 2 and <= 10;
            }

            bool allLatin = name.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
            if (allLatin)
            {
                return name.Length is >= 2 and <= 20;
            }

            return false;
        }
    }
}
