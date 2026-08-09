using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Application.Auth.Commands;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Settings.Commands;
using PlayGround.Contracts.Auth;
using PlayGround.Contracts.Common;
using PlayGround.Contracts.Settings;
using PlayGround.Infrastructure.Logging;
using PlayGround.Server.Services;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

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
        private readonly DisplayNameChangeCommand mDisplayNameChange;
        private readonly SocialLinkCommand mSocialLink;
        private readonly ITokenRevocationStore mTokenRevocation;

        public AuthController(
            OAuthService oauth,
            LoginBySocialCommand loginBySocial,
            LoginByEmailCommand loginByEmail,
            AccountSettingsCommand accountSettings,
            NotificationPreferenceCommand notificationPreference,
            AccountDeleteCommand accountDelete,
            DisplayNameChangeCommand displayNameChange,
            SocialLinkCommand socialLink,
            ITokenRevocationStore tokenRevocation)
        {
            mOAuth = oauth;
            mLoginBySocial = loginBySocial;
            mLoginByEmail = loginByEmail;
            mAccountSettings = accountSettings;
            mNotificationPreference = notificationPreference;
            mAccountDelete = accountDelete;
            mDisplayNameChange = displayNameChange;
            mSocialLink = socialLink;
            mTokenRevocation = tokenRevocation;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : Guid.Empty;

        /// <summary>현재 토큰의 만료 시각 — 무효화 기록을 그때까지만 들고 있으면 된다.
        /// exp를 못 읽으면 액세스 토큰 최대 수명만큼 보수적으로 잡는다.</summary>
        private DateTimeOffset CurrentTokenExpiresAt =>
            long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Exp), out long seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : SystemTime.OffsetNow.AddHours(1);

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
                Role = Enum.TryParse(User.FindFirstValue(ClaimTypes.Role), out AccountRole role) ? role : AccountRole.General,
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

        /// <summary>로그아웃 — **이 토큰만** 무효화한다(다른 기기 세션은 유지).
        /// 클라이언트가 로컬 토큰을 지우는 것만으로는 이미 나간 토큰이 남은 수명 동안 계속 통한다.</summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<Envelope<bool>> LogoutAsync(CancellationToken cancellation)
        {
            string tokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? string.Empty;
            await mTokenRevocation.RevokeTokenAsync(tokenId, CurrentTokenExpiresAt, cancellation);

            KeyValueLogExtensions.Info(Logger, "Logout", ("UserId", CurrentUserId));
            return Result<bool>.Success(true).ToEnvelope();
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
                return result.ToEnvelope();
            }

            // 탈퇴는 기기 하나가 아니라 **그 사용자의 토큰 전부**를 끊는다
            await mTokenRevocation.RevokeAllForUserAsync(CurrentUserId, SystemTime.OffsetNow, cancellation);

            return result.ToEnvelope();
        }

        /// <summary>이름 변경 (Design.SettingsFlows ①). 성공 시 갱신된 name 클레임의 새 토큰을 돌려준다 —
        /// 클라이언트가 토큰을 교체하면 GNB·프로필이 즉시 반영된다(역할 승격 재발급과 같은 패턴).</summary>
        [Authorize]
        [HttpPut("me/display-name")]
        public async Task<Envelope<AuthResult>> ChangeDisplayNameAsync(
            [FromBody] ChangeDisplayNameRequest request, CancellationToken cancellation)
        {
            Result<AuthResult> result = await mDisplayNameChange.ExecuteAsync(CurrentUserId, request.DisplayName, cancellation);
            if (result.IsError)
            {
                result.LogWith(Logger, "ChangeDisplayName");
            }

            return result.ToEnvelope();
        }

        /// <summary>로그인 수단 연결 시작 (Design.SettingsFlows ②). 현재 로그인 사용자를 서명 상태에 실어
        /// OAuth 인가 URL을 돌려준다 — 클라이언트가 그 URL로 이동한다(콜백이 연결 모드로 분기). 신규 흐름 없음.</summary>
        [Authorize]
        [HttpGet("social/{provider}/link")]
        public Envelope<string> SocialLinkStart(string provider)
        {
            if (!mOAuth.IsSupported(provider) || provider.ToLowerInvariant() == "line")
            {
                return Result<string>.Error(ErrorCode.InvalidInput, "unsupported provider").ToEnvelope();
            }

            if (!mOAuth.IsConfigured(provider))
            {
                return Result<string>.Error(ErrorCode.OperationFailed, "provider not configured").ToEnvelope();
            }

            string state = mOAuth.CreateLinkState(CurrentUserId);
            KeyValueLogExtensions.Info(Logger, "Social link started", ("Provider", provider), ("UserId", CurrentUserId));
            return Result<string>.Success(mOAuth.GetAuthorizationUrl(provider, state)).ToEnvelope();
        }

        /// <summary>로그인 수단 해제 (Design.SettingsFlows ②). **마지막 1개는 SP가 거부**('LastMeans').
        /// 상태 문자열을 그대로 돌려준다: 'Ok'|'LastMeans'|'NotLinked'.</summary>
        [Authorize]
        [HttpDelete("me/social/{provider}")]
        public async Task<Envelope<string>> UnlinkSocialAsync(string provider, CancellationToken cancellation)
        {
            Result<string> result = await mSocialLink.UnlinkAsync(CurrentUserId, provider, cancellation);
            if (result.IsError)
            {
                result.LogWith(Logger, "UnlinkSocial");
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
                KeyValueLogExtensions.Warn(Logger, "Social login provider not configured", ("Provider", provider));
                return Redirect("/login?error=NotConfigured");
            }

            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            KeyValueLogExtensions.Info(Logger, "Social login started", ("Provider", provider));
            return Redirect(mOAuth.GetAuthorizationUrl(provider, state));
        }

        /// <summary>provider 콜백 → 코드 교환 → 로그인(find-or-create) → 토큰을 URL fragment로 전달(로그·리퍼러 미노출).</summary>
        [HttpGet("social/{provider}/callback")]
        public async Task<IActionResult> SocialCallbackAsync(string provider, [FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                KeyValueLogExtensions.Warn(Logger, "Social callback missing code", ("Provider", provider));
                return Redirect("/login?error=NoCode");
            }

            var userInfo = await mOAuth.GetUserInfoAsync(provider, code);
            if (userInfo is null)
            {
                KeyValueLogExtensions.Warn(Logger, "Social callback provider error", ("Provider", provider));
                return Redirect("/login?error=ProviderError");
            }

            // 연결(link) 모드 — state가 서명된 링크 토큰이면 로그인(find-or-create) 대신 현재 계정에 붙인다.
            if (mOAuth.TryReadLinkState(state, out Guid linkUserId))
            {
                Result<string> link = await mSocialLink.LinkAsync(
                    linkUserId, userInfo.Provider, userInfo.ProviderUserId, userInfo.Email, cancellation);
                if (link.IsError)
                {
                    link.LogWith(Logger, "LinkSocialCallback");
                    return Redirect("/settings/account?linkError=Failed");
                }

                KeyValueLogExtensions.Info(Logger, "Social link completed", ("Provider", provider), ("Status", link.Value), ("UserId", linkUserId));
                // 'Conflict' = 다른 계정에 이미 연결 → 인라인 오류 / 'Ok'·'AlreadyLinked' = 성공 토스트
                return link.Value == "Conflict"
                    ? Redirect($"/settings/account?linkError=Duplicate&provider={Uri.EscapeDataString(userInfo.Provider)}")
                    : Redirect($"/settings/account?linked={Uri.EscapeDataString(userInfo.Provider)}");
            }

            var result = await mLoginBySocial.ExecuteAsync(
                userInfo.Provider, userInfo.ProviderUserId, userInfo.Email, userInfo.FullName, userInfo.ProfileImageUrl, cancellation);

            if (result.IsError)
            {
                result.LogWith(Logger, "LoginBySocial");
                return Redirect("/login?error=LoginFailed");
            }

            KeyValueLogExtensions.Info(Logger, "Social login completed", ("Provider", provider), ("UserId", result.Value!.User.UserId));

            return Redirect($"/settings/select-role#access_token={Uri.EscapeDataString(result.Value!.AccessToken)}");
        }
    }
}
