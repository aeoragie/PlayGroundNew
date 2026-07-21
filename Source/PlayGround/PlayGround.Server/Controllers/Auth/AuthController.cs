using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Auth;
using PlayGround.Contracts.Settings;
using PlayGround.Application.Auth.Commands;
using PlayGround.Application.Settings.Commands;
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
        private readonly LoginByEmailCommand mLoginByEmail;
        private readonly AccountSettingsCommand mAccountSettings;
        private readonly NotificationPreferenceCommand mNotificationPreference;
        private readonly AccountDeleteCommand mAccountDelete;

        public AuthController(
            OAuthService oauth,
            LoginBySocialCommand loginBySocial,
            LoginByEmailCommand loginByEmail,
            AccountSettingsCommand accountSettings,
            NotificationPreferenceCommand notificationPreference,
            AccountDeleteCommand accountDelete)
        {
            mOAuth = oauth;
            mLoginBySocial = loginBySocial;
            mLoginByEmail = loginByEmail;
            mAccountSettings = accountSettings;
            mNotificationPreference = notificationPreference;
            mAccountDelete = accountDelete;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : Guid.Empty;

        /// <summary>이메일 로그인/가입 (없으면 자동 생성). 성공 시 액세스 토큰 반환.</summary>
        [HttpPost("login/email")]
        public async Task<Envelope<AuthResult>> LoginByEmailAsync(
            [FromBody] LoginByEmailRequest request, CancellationToken cancellation)
        {
            Result<AuthResult> result = await mLoginByEmail.ExecuteAsync(request.Email, request.Password, cancellation);
            if (result.IsError)
            {
                result.LogWith(Logger, "LoginByEmail");
            }

            return result.ToEnvelope();
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

        /// <summary>계정 설정 묶음 (설정 · 계정 탭). 이메일은 서버에서 마스킹된 값만.</summary>
        [Authorize]
        [HttpGet("me/settings")]
        public async Task<Envelope<AccountSettingsResponse>> GetSettingsAsync(CancellationToken cancellation)
        {
            Result<AccountSettingsResponse> result = await mAccountSettings.ExecuteAsync(CurrentUserId, cancellation);
            if (result.IsError)
            {
                result.LogWith(Logger, "GetAccountSettings");
            }

            return result.ToEnvelope();
        }

        /// <summary>알림 설정 — 6개 항목 전부(저장값 없으면 기본값). 승인형은 목록에 없다(항상 켜짐).</summary>
        [Authorize]
        [HttpGet("me/notifications")]
        public async Task<Envelope<NotificationPreferencesResponse>> GetNotificationsAsync(CancellationToken cancellation)
        {
            Result<NotificationPreferencesResponse> result = await mNotificationPreference.GetAsync(CurrentUserId, cancellation);
            if (result.IsError)
            {
                result.LogWith(Logger, "GetNotificationPreferences");
            }

            return result.ToEnvelope();
        }

        /// <summary>알림 설정 변경 — 승인형 항목은 서버가 거부한다(InvalidInput).</summary>
        [Authorize]
        [HttpPut("me/notifications")]
        public async Task<Envelope<bool>> SetNotificationAsync(
            [FromBody] SetNotificationPreferenceRequest request, CancellationToken cancellation)
        {
            Result<bool> result = await mNotificationPreference.SetAsync(CurrentUserId, request, cancellation);
            if (result.IsError)
            {
                result.LogWith(Logger, "SetNotificationPreference");
            }

            return result.ToEnvelope();
        }

        /// <summary>계정 삭제 (소프트 삭제). 클라이언트는 성공 시 로그아웃 → 랜딩으로 보낸다.</summary>
        [Authorize]
        [HttpDelete("me")]
        public async Task<Envelope<bool>> DeleteAccountAsync(CancellationToken cancellation)
        {
            Result<bool> result = await mAccountDelete.ExecuteAsync(CurrentUserId, cancellation);
            if (result.IsError)
            {
                result.LogWith(Logger, "DeleteAccount");
            }

            return result.ToEnvelope();
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
