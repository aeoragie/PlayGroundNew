using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Auth.Commands
{
    /// <summary>로그인 수단 연결·해제 유즈케이스 (Design.SettingsFlows ②). OAuth 코드 교환은 기존 파이프라인
    /// (OAuthService) 재사용 — 여기서는 이미 확인된 신원을 현재 로그인 계정에 붙이거나 뗀다.
    /// 상태 문자열을 그대로 돌려주고(호출측이 리다이렉트/응답으로 해석): 연결 'Ok'|'AlreadyLinked'|'Conflict',
    /// 해제 'Ok'|'LastMeans'|'NotLinked'. **마지막 1개 해제 불가**는 SP가 원자 판정한다.</summary>
    public class SocialLinkCommand
    {
        private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase) { "Google", "Kakao" };

        private readonly IAccountRepository mRepository;

        private readonly ILogger<SocialLinkCommand> mLogger;

        public SocialLinkCommand(IAccountRepository repository, ILogger<SocialLinkCommand> logger)
        {
            Debug.Assert(repository != null);
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<string>> LinkAsync(
            Guid userId, string provider, string providerUserId, string? email, CancellationToken cancellation = default) =>
            (await LinkCoreAsync(userId, provider, providerUserId, email, cancellation)).LogWith(mLogger, "Link", ("UserId", userId));

        private async Task<Result<string>> LinkCoreAsync(
            Guid userId, string provider, string providerUserId, string? email, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<string>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (!Supported.Contains(provider) || string.IsNullOrWhiteSpace(providerUserId))
            {
                return Result<string>.Error(ErrorCode.InvalidInput, "unsupported provider");
            }

            return await mRepository.LinkSocialAsync(userId, Normalize(provider), providerUserId, email, cancellation);
        }

        public async Task<Result<string>> UnlinkAsync(Guid userId, string provider, CancellationToken cancellation = default) =>
            (await UnlinkCoreAsync(userId, provider, cancellation)).LogWith(mLogger, "Unlink", ("UserId", userId));

        private async Task<Result<string>> UnlinkCoreAsync(Guid userId, string provider, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<string>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (!Supported.Contains(provider))
            {
                return Result<string>.Error(ErrorCode.InvalidInput, "unsupported provider");
            }

            return await mRepository.UnlinkSocialAsync(userId, Normalize(provider), cancellation);
        }

        // DB 저장값은 'Google'/'Kakao' (첫 글자 대문자) — 소셜 계정 테이블 규약과 맞춘다
        private static string Normalize(string provider) =>
            char.ToUpperInvariant(provider[0]) + provider[1..].ToLowerInvariant();
    }
}
