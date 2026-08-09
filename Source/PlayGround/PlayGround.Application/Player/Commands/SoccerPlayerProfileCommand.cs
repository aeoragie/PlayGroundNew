using Microsoft.Extensions.Logging;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Models;
using PlayGround.Contracts.Player;
using PlayGround.Contracts.Soccer;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;
using System.Globalization;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 온보딩 프로필 생성 유즈케이스. 입력 검증·정규화 후 포트로 저장.</summary>
    public class SoccerPlayerProfileCommand
    {
        private readonly IPlayerRepository mRepository;
        private readonly IAccountRepository mAccountRepository;
        private readonly IJwtTokenService mTokenService;
        private readonly ILogger<SoccerPlayerProfileCommand> mLogger;

        public SoccerPlayerProfileCommand(IPlayerRepository repository, IAccountRepository accountRepository, IJwtTokenService tokenService, ILogger<SoccerPlayerProfileCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            Debug.Assert(accountRepository != null, "accountRepository is required");
            Debug.Assert(tokenService != null, "tokenService is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mAccountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            mTokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CreatePlayerProfileResponse>> ExecuteAsync(
            Guid userId, CreatePlayerProfileRequest request, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, request, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<CreatePlayerProfileResponse>> ExecuteCoreAsync(
            Guid userId, CreatePlayerProfileRequest request, CancellationToken cancellation = default)
        {
            Debug.Assert(request != null, "request is required");
            if (request is null)
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.InvalidInput, "request is null");
            }

            if (userId == Guid.Empty)
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.MissingRequired, "Name is required");
            }

            DateOnly? birthDate = null;
            if (!string.IsNullOrWhiteSpace(request.BirthDate))
            {
                if (!DateOnly.TryParseExact(request.BirthDate.Trim(), "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed))
                {
                    return Result<CreatePlayerProfileResponse>.Error(ErrorCode.InvalidDateFormat, "BirthDate must be YYYY.MM.DD");
                }
                birthDate = parsed;
            }

            if (request.AgeGroup == SoccerAgeGroup.Unknown)
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.OutOfRange, "invalid age group");
            }

            var input = new CreatePlayerInput
            {
                UserId = userId,
                Name = request.Name.Trim(),
                BirthDate = birthDate,
                AgeGroup = request.AgeGroup,
                Region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim()
            };

            Result<Guid> created = await mRepository.CreateAsync(input, cancellation);
            if (created.IsError)
            {
                return Result<CreatePlayerProfileResponse>.Failure(created.ResultData);
            }

            mLogger.Info("Player profile created", ("UserId", userId));

            // 온보딩 완료 → 역할 승격 + 승격된 역할로 JWT 재발급 (재로그인 없이 /dashboard 분기가 맞도록).
            // 실패해도 프로필은 생성됐으므로 비치명적 — 토큰 없이 반환하면 기존 토큰이 유지된다.
            Result<AccountUser> promoted = await mAccountRepository.UpdateRoleAsync(userId, "Player", cancellation);

            string? accessToken = null;
            if (promoted.IsSuccess)
            {
                AccountUser user = promoted.Value;
                accessToken = mTokenService.GenerateAccessToken(
                    user.UserId, user.Email, user.DisplayName, user.UserRole.ToString(), user.ProfileImageUrl);
                mLogger.Info("User role promoted", ("UserId", userId), ("Role", "Player"));
            }
            else
            {
                promoted.LogWith(mLogger, "PromoteToPlayer", ("UserId", userId));
            }

            return Result<CreatePlayerProfileResponse>.Success(new CreatePlayerProfileResponse
            {
                PlayerId = created.Value,
                AccessToken = accessToken
            });
        }

    }
}
