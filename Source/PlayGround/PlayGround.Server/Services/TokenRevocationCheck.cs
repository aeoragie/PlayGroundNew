using Microsoft.AspNetCore.Authentication.JwtBearer;
using PlayGround.Application.Interfaces;
using PlayGround.Infrastructure.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// Bearer 검증 마지막 단계 — 서명·만료를 통과한 토큰이 **무효화된 것은 아닌지** 본다.
    /// JWT는 상태가 없어 로그아웃·탈퇴 뒤에도 남은 수명 동안 통하므로 이 관문이 필요하다.
    /// </summary>
    public static class TokenRevocationCheck
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static async Task OnTokenValidatedAsync(TokenValidatedContext context)
        {
            ITokenRevocationStore? store = context.HttpContext.RequestServices
                .GetService<ITokenRevocationStore>();
            if (store is null)
            {
                return; // 저장소 미구성 — 무효화 기능 없음
            }

            ClaimsPrincipal? principal = context.Principal;
            if (principal is null)
            {
                return;
            }

            string tokenId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? string.Empty;
            Guid userId = ReadUserId(principal);
            DateTimeOffset issuedAt = ReadIssuedAt(principal);

            bool revoked = await store.IsRevokedAsync(
                tokenId, userId, issuedAt, context.HttpContext.RequestAborted);
            if (!revoked)
            {
                return;
            }

            Logger.InfoWith("Revoked token rejected", ("UserId", userId));
            context.Fail("token revoked");
        }

        /// <summary>토큰의 sub. 형식이 아니면 Guid.Empty — 사용자 단위 판정만 건너뛴다.</summary>
        private static Guid ReadUserId(ClaimsPrincipal principal)
        {
            string? value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out Guid userId) ? userId : Guid.Empty;
        }

        /// <summary>토큰의 iat(Unix 초). 없으면 MinValue — 어떤 기준선보다 앞서므로
        /// 사용자 단위 무효화가 걸리면 걸러진다(안전한 쪽).</summary>
        private static DateTimeOffset ReadIssuedAt(ClaimsPrincipal principal)
        {
            string? value = principal.FindFirstValue(JwtRegisteredClaimNames.Iat);
            return long.TryParse(value, out long seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : DateTimeOffset.MinValue;
        }
    }
}
