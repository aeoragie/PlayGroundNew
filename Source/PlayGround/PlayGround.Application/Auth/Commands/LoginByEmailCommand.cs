using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Auth;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Auth.Commands
{
    /// <summary>이메일 로그인/가입 유즈케이스 (find-or-create). 없으면 비밀번호 해시로 신규 생성,
    /// 있으면 해시 검증. 소셜 전용 계정(PasswordHash 없음)은 이메일 로그인 불가.</summary>
    public class LoginByEmailCommand
    {
        private const int MinPasswordLength = 8;

        private readonly IAccountRepository mRepository;
        private readonly IJwtTokenService mTokenService;
        private readonly IPasswordHasher mPasswordHasher;

        public LoginByEmailCommand(IAccountRepository repository, IJwtTokenService tokenService, IPasswordHasher passwordHasher)
        {
            Debug.Assert(repository != null && tokenService != null && passwordHasher != null);
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mTokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            mPasswordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<Result<AuthResult>> ExecuteAsync(string email, string password, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return Result<AuthResult>.Error(ErrorCode.MissingRequired, "email/password required");
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();
            if (!normalizedEmail.Contains('@'))
            {
                return Result<AuthResult>.Error(ErrorCode.InvalidEmail, "이메일 형식이 올바르지 않아요.");
            }

            Result<AccountUser?> existing = await mRepository.GetByEmailAsync(normalizedEmail, cancellation);
            if (existing.IsError)
            {
                return Result<AuthResult>.Failure(existing.ResultData);
            }

            AccountUser? user = existing.Value;

            //.// 기존 계정 — 비밀번호 검증
            if (user is not null)
            {
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    return Result<AuthResult>.Error(ErrorCode.InvalidCredentials, "이 이메일은 소셜 로그인으로 가입된 계정이에요.");
                }

                if (!mPasswordHasher.Verify(user.PasswordHash, password))
                {
                    return Result<AuthResult>.Error(ErrorCode.InvalidCredentials, "이메일 또는 비밀번호가 올바르지 않아요.");
                }

                return Result<AuthResult>.Success(BuildResult(user));
            }

            //.// 신규 가입 — 최소 길이 검증 후 해시 생성
            if (password.Length < MinPasswordLength)
            {
                return Result<AuthResult>.Error(ErrorCode.InvalidInput, $"비밀번호는 {MinPasswordLength}자 이상이어야 해요.");
            }

            string hash = mPasswordHasher.Hash(password);
            string displayName = normalizedEmail.Split('@')[0];

            Result<AccountUser> created = await mRepository.CreateByEmailAsync(normalizedEmail, hash, displayName, cancellation);
            if (created.IsError)
            {
                return Result<AuthResult>.Failure(created.ResultData);
            }

            return Result<AuthResult>.Success(BuildResult(created.Value));
        }

        private AuthResult BuildResult(AccountUser user)
        {
            string accessToken = mTokenService.GenerateAccessToken(
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
