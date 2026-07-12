using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Auth;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Auth.Commands
{
    /// <summary>소셜 로그인 유즈케이스: (Provider, ProviderUserId)로 find-or-create → JWT 발급.
    /// 신규 사용자는 General 역할로 자동 가입(온보딩에서 역할 선택).</summary>
    public class LoginBySocialCommand
    {
        private readonly IAccountRepository Repository;
        private readonly IJwtTokenService TokenService;

        public LoginBySocialCommand(IAccountRepository repository, IJwtTokenService tokenService)
        {
            Debug.Assert(repository != null, "repository is required");
            Debug.Assert(tokenService != null, "tokenService is required");
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            TokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<Result<AuthResult>> ExecuteAsync(
            string provider, string providerUserId, string? email, string? displayName, string? profileImageUrl,
            CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerUserId))
            {
                return Result<AuthResult>.Error(ErrorCode.MissingRequired, "provider/providerUserId required");
            }

            // 1) 기존 소셜 연동 조회
            var existing = await Repository.GetBySocialAsync(provider, providerUserId, cancellation);
            if (existing.IsError)
            {
                return Result<AuthResult>.Failure(existing.ResultData);
            }

            var user = existing.Value;

            // 2) 없으면 신규 생성 (General 자동 가입)
            if (user is null)
            {
                var created = await Repository.CreateWithSocialAsync(
                    email ?? $"{providerUserId}@{provider.ToLowerInvariant()}.social",
                    string.IsNullOrWhiteSpace(displayName) ? "사용자" : displayName,
                    provider, providerUserId, profileImageUrl, cancellation);
                if (created.IsError)
                {
                    return Result<AuthResult>.Failure(created.ResultData);
                }
                user = created.Value;
            }

            return Result<AuthResult>.Success(BuildResult(user!));
        }

        private AuthResult BuildResult(AccountUser user)
        {
            var accessToken = TokenService.GenerateAccessToken(
                user.UserId, user.Email, user.DisplayName, user.UserRole, user.ProfileImageUrl);

            return new AuthResult
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
            };
        }
    }
}
