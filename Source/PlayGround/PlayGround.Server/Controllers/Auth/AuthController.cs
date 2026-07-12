using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Auth;
using PlayGround.Application.Auth.Commands;
using PlayGround.Server.Services;

namespace PlayGround.Server.Controllers.Auth
{
    /// <summary>인증(공유 — 종목 무관). 소셜 OAuth 시작/콜백 + 현재 사용자(me).</summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly OAuthService mOAuth;
        private readonly LoginBySocialCommand mLoginBySocial;

        public AuthController(OAuthService oauth, LoginBySocialCommand loginBySocial)
        {
            mOAuth = oauth;
            mLoginBySocial = loginBySocial;
        }

        /// <summary>현재 로그인 사용자 — 인증 토큰 클레임을 반환. 클라이언트의 로그인 후 라우팅에 사용.</summary>
        [Authorize]
        [HttpGet("me")]
        public Envelope<AuthUserDto> Me()
        {
            var user = new AuthUserDto
            {
                UserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : Guid.Empty,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                DisplayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? string.Empty,
                Role = User.FindFirstValue(ClaimTypes.Role) ?? "General",
                ProfileImageUrl = User.FindFirstValue("avatar")
            };
            return Result<AuthUserDto>.Success(user).ToEnvelope();
        }

        [HttpGet("social/{provider}")]
        public IActionResult SocialStart(string provider)
        {
            if (!mOAuth.IsSupported(provider))
            {
                return BadRequest($"Unsupported provider: {provider}");
            }

            if (!mOAuth.IsConfigured(provider))
            {
                // 자격증명 미설정(예: LINE 키 미발급) — 500 대신 로그인 화면으로 안내.
                Logger.WarnWith("Social login provider not configured", ("Provider", provider));
                return Redirect("/login?error=NotConfigured");
            }

            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            Logger.InfoWith("Social login started", ("Provider", provider));
            return Redirect(mOAuth.GetAuthorizationUrl(provider, state));
        }

        /// <summary>provider 콜백 → 코드 교환 → 로그인(find-or-create) → 토큰을 URL fragment로 전달(로그·리퍼러 미노출).</summary>
        [HttpGet("social/{provider}/callback")]
        public async Task<IActionResult> SocialCallbackAsync(string provider, [FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                Logger.WarnWith("Social callback missing code", ("Provider", provider));
                return Redirect("/login?error=NoCode");
            }

            var userInfo = await mOAuth.GetUserInfoAsync(provider, code);
            if (userInfo is null)
            {
                Logger.WarnWith("Social callback provider error", ("Provider", provider));
                return Redirect("/login?error=ProviderError");
            }

            var result = await mLoginBySocial.ExecuteAsync(
                userInfo.Provider, userInfo.ProviderUserId, userInfo.Email, userInfo.FullName, userInfo.ProfileImageUrl, cancellation);

            if (result.IsError)
            {
                result.LogWith(Logger, "LoginBySocial");
                return Redirect("/login?error=LoginFailed");
            }

            Logger.InfoWith("Social login completed", ("Provider", provider), ("UserId", result.Value!.User.UserId));

            return Redirect($"/settings/select-role#access_token={Uri.EscapeDataString(result.Value!.AccessToken)}");
        }
    }
}
