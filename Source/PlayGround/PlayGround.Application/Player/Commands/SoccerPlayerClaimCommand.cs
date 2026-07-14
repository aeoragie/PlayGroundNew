using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>초대코드 Claim 유즈케이스 — 로스터 선수 프로필을 계정에 연결하고 필요 시 Player로 승격.</summary>
    public class SoccerPlayerClaimCommand
    {
        private readonly IPlayerRepository mRepository;
        private readonly IAccountRepository mAccountRepository;
        private readonly IJwtTokenService mTokenService;

        public SoccerPlayerClaimCommand(IPlayerRepository repository, IAccountRepository accountRepository, IJwtTokenService tokenService)
        {
            Debug.Assert(repository != null, "repository is required");
            Debug.Assert(accountRepository != null, "accountRepository is required");
            Debug.Assert(tokenService != null, "tokenService is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mAccountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            mTokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<Result<ClaimPlayerInviteResponse>> ExecuteAsync(
            Guid userId, string code, string? currentRole, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<ClaimPlayerInviteResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            string normalized = code?.Trim().ToUpperInvariant() ?? string.Empty;
            if (normalized.Length is < 4 or > 12)
            {
                return Result<ClaimPlayerInviteResponse>.Error(ErrorCode.InvalidInput, "invalid code format");
            }

            Result<ClaimPlayerInviteResponse?> claimed = await mRepository.ClaimInviteAsync(userId, normalized, cancellation);
            if (claimed.IsError)
            {
                return Result<ClaimPlayerInviteResponse>.Failure(claimed.ResultData);
            }

            if (claimed.Value is null)
            {
                // 무효·만료·사용된 코드, 이미 연결된 선수 — 사유는 서버 로그로만 구분
                return Result<ClaimPlayerInviteResponse>.Error(ErrorCode.NotFound, "invite code is not valid");
            }

            // Claim 완료 → General 계정만 Player로 승격 + JWT 재발급 (TeamAdmin 등 상위 역할은 강등 금지).
            // 승격 실패해도 연결은 완료됐으므로 비치명적 — 토큰 없이 반환하면 기존 토큰이 유지된다.
            ClaimPlayerInviteResponse response = claimed.Value;
            if (currentRole == "General")
            {
                Result<AccountUser> promoted = await mAccountRepository.UpdateRoleAsync(userId, "Player", cancellation);
                if (promoted.IsSuccess)
                {
                    AccountUser user = promoted.Value;
                    response.AccessToken = mTokenService.GenerateAccessToken(
                        user.UserId, user.Email, user.DisplayName, user.UserRole, user.ProfileImageUrl);
                }
            }

            return Result<ClaimPlayerInviteResponse>.Success(response);
        }
    }
}
