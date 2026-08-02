using System.Diagnostics;
using System.Globalization;
using PlayGround.Infrastructure.Logging;
using PlayGround.Infrastructure.Store;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 토큰 무효화 상태를 Redis에 둔다. 서버가 여러 대여도 같은 판정을 내려야 하고,
    /// 재시작해도 남아야 하므로 인메모리로는 안 된다.
    ///
    /// **Redis를 못 쓰면 "무효 아님"으로 답한다(fail-open).** 막는 쪽으로 가면 Redis 장애가
    /// 곧바로 전체 로그인 불가가 된다. 액세스 토큰 수명이 짧아(기본 30분) 노출 창이 그만큼으로
    /// 제한되는 것과 저울질한 결과다. 대신 그 상황을 Warn으로 남긴다.
    /// </summary>
    public sealed class RedisTokenRevocationStore : ITokenRevocationStore
    {
        /// <summary>RedisConfig:Connections 의 이름 — appsettings의 값과 맞춘다.</summary>
        public const string ConnectionName = "Auth";

        private const string TokenKeyPrefix = "auth:revoked:token:";
        private const string UserKeyPrefix = "auth:revoked:user:";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>사용자 단위 기준선 보관 기간 — 액세스 토큰 최대 수명보다 넉넉히 잡는다.
        /// 이보다 오래된 토큰은 어차피 만료라 기준선을 들고 있을 이유가 없다.</summary>
        private readonly TimeSpan mUserRevocationRetention;

        private readonly RedisService mRedis;

        public RedisTokenRevocationStore(RedisService redis, IConfiguration configuration)
        {
            Debug.Assert(redis != null, "redis is required");
            mRedis = redis ?? throw new ArgumentNullException(nameof(redis));

            int accessTokenMinutes = configuration.GetValue("Jwt:AccessTokenExpirationMinutes", 30);
            mUserRevocationRetention = TimeSpan.FromMinutes(accessTokenMinutes + 5);
        }

        public async Task RevokeTokenAsync(
            string tokenId, DateTimeOffset expiresAt, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
            {
                Logger.WarnWith("Token revocation skipped — empty token id");
                return;
            }

            TimeSpan remaining = expiresAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return; // 이미 만료 — 기억할 필요가 없다
            }

            await using IRedisSession? session = mRedis.CreateSession(ConnectionName);
            if (session is null)
            {
                Logger.ErrorWith("Token revocation failed — Redis unavailable", ("TokenId", tokenId));
                return;
            }

            RedisResult<bool> stored = await session.TryStringSetAsync(
                TokenKeyPrefix + tokenId, "1", remaining);

            if (!stored.IsSuccess)
            {
                Logger.ErrorWith("Token revocation not stored", ("TokenId", tokenId));
                return;
            }

            Logger.InfoWith("Token revoked", ("TokenId", tokenId));
        }

        public async Task RevokeAllForUserAsync(
            Guid userId, DateTimeOffset revokedAt, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                Logger.WarnWith("User token revocation skipped — empty user");
                return;
            }

            await using IRedisSession? session = mRedis.CreateSession(ConnectionName);
            if (session is null)
            {
                Logger.ErrorWith("User token revocation failed — Redis unavailable", ("UserId", userId));
                return;
            }

            string stamp = revokedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            RedisResult<bool> stored = await session.TryStringSetAsync(
                UserKeyPrefix + userId, stamp, mUserRevocationRetention);

            if (!stored.IsSuccess)
            {
                Logger.ErrorWith("User token revocation not stored", ("UserId", userId));
                return;
            }

            Logger.InfoWith("All tokens revoked for user", ("UserId", userId));
        }

        public async Task<bool> IsRevokedAsync(
            string tokenId, Guid userId, DateTimeOffset issuedAt, CancellationToken cancellation = default)
        {
            await using IRedisSession? session = mRedis.CreateSession(ConnectionName);
            if (session is null)
            {
                // fail-open — 클래스 주석 참조. 막으면 Redis 장애가 전체 인증 실패로 번진다.
                Logger.WarnWith("Revocation check skipped — Redis unavailable", ("UserId", userId));
                return false;
            }

            if (!string.IsNullOrWhiteSpace(tokenId))
            {
                RedisResult<bool> exists = await session.TryKeyExistsAsync(TokenKeyPrefix + tokenId);
                if (exists.IsSuccess && exists.Value)
                {
                    return true;
                }
            }

            if (userId == Guid.Empty)
            {
                return false;
            }

            RedisResult<string> cutoff = await session.TryStringGetAsync(UserKeyPrefix + userId);
            if (!cutoff.IsSuccess || string.IsNullOrEmpty(cutoff.Value)
                || !long.TryParse(cutoff.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long seconds))
            {
                return false;
            }

            // 기준선보다 먼저 발급된 토큰만 자른다 — 이후 재발급분(역할 승격 등)은 살아 있어야 한다
            return issuedAt < DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
    }
}
