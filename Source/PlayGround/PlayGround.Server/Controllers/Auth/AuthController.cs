using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Infrastructure.Logging;
using PlayGround.Application.Auth.Commands;
using PlayGround.Server.Services;

namespace PlayGround.Server.Controllers.Auth
{
    /// <summary>인증(공유 — 종목 무관). 소셜 OAuth 시작/콜백.</summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly OAuthService OAuth;
        private readonly LoginBySocialCommand LoginBySocial;

        public AuthController(OAuthService oauth, LoginBySocialCommand loginBySocial)
        {
            OAuth = oauth;
            LoginBySocial = loginBySocial;
        }

        /// <summary>소셜 로그인 시작 → provider 인증 페이지로 리다이렉트.</summary>
        [HttpGet("social/{provider}")]
        public IActionResult SocialStart(string provider)
        {
            if (!OAuth.IsSupported(provider))
            {
                return BadRequest($"Unsupported provider: {provider}");
            }

            if (!OAuth.IsConfigured(provider))
            {
                // 자격증명 미설정(예: LINE 키 미발급) — 500 대신 로그인 화면으로 안내.
                Logger.WarnWith("Social login provider not configured", ("Provider", provider));
                return Redirect("/login?error=NotConfigured");
            }

            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            Logger.InfoWith("Social login started", ("Provider", provider));
            return Redirect(OAuth.GetAuthorizationUrl(provider, state));
        }

        /// <summary>provider 콜백 → 코드 교환 → find-or-create + JWT → 클라이언트로 리다이렉트.
        /// 토큰은 URL fragment(#access_token=)로 전달 — 서버 로그·리퍼러에 남지 않음.</summary>
        [HttpGet("social/{provider}/callback")]
        public async Task<IActionResult> SocialCallbackAsync(string provider, [FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                Logger.WarnWith("Social callback missing code", ("Provider", provider));
                return Redirect("/login?error=NoCode");
            }

            var userInfo = await OAuth.GetUserInfoAsync(provider, code);
            if (userInfo is null)
            {
                Logger.WarnWith("Social callback provider error", ("Provider", provider));
                return Redirect("/login?error=ProviderError");
            }

            var result = await LoginBySocial.ExecuteAsync(
                userInfo.Provider, userInfo.ProviderUserId, userInfo.Email, userInfo.FullName, userInfo.ProfileImageUrl, cancellation);

            if (result.IsError)
            {
                result.LogWith(Logger, "LoginBySocial");
                return Redirect("/login?error=LoginFailed");
            }

            Logger.InfoWith("Social login completed", ("Provider", provider), ("UserId", result.Value!.User.UserId));

            // 로그인 성공 → 역할 선택으로. 토큰은 fragment로 전달(SPA가 읽어 저장 — 2d).
            return Redirect($"/settings/select-role#access_token={Uri.EscapeDataString(result.Value!.AccessToken)}");
        }
    }
}
