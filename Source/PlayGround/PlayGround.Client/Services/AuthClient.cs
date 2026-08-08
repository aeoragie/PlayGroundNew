using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Auth;
using PlayGround.Contracts.Settings;

using PlayGround.Client.Localization;

namespace PlayGround.Client.Services
{
    /// <summary>현재 사용자 조회. 인증 토큰은 공유 HttpClient 기본 헤더로 자동 부착됨.</summary>
    public class AuthClient
    {
        private readonly HttpClient mHttp;

        public AuthClient(HttpClient http)
        {
            mHttp = http;
        }

        public async Task<AuthUserDto?> GetMeAsync()
        {
            try
            {
                Envelope<AuthUserDto>? envelope = await mHttp.GetFromJsonAsync<Envelope<AuthUserDto>>("api/auth/me");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        public async Task<EmailLoginResult> LoginByEmailAsync(string email, string password)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    "api/auth/login/email", new LoginByEmailRequest { Email = email, Password = password });

                Envelope<AuthResult>? envelope = await response.Content.ReadFromJsonAsync<Envelope<AuthResult>>();
                if (envelope is { IsSuccess: true, Data: not null })
                {
                    return new EmailLoginResult(true, envelope.Data.AccessToken, null);
                }

                return new EmailLoginResult(false, null, envelope?.Message ?? AppText.Errors.LoginFailed);
            }
            catch
            {
                return new EmailLoginResult(false, null, AppText.Errors.LoginNetwork);
            }
        }

        //.// 설정 (Design.Settings)

        /// <summary>계정 설정 묶음. 오류 시 null — 호출부가 실패 상태로 처리한다.</summary>
        public async Task<AccountSettingsResponse?> GetSettingsAsync()
        {
            try
            {
                Envelope<AccountSettingsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<AccountSettingsResponse>>("api/auth/me/settings");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>알림 설정 — 항목 전부(저장값 없으면 기본값). 오류 시 null.</summary>
        public async Task<NotificationPreferencesResponse?> GetNotificationsAsync()
        {
            try
            {
                Envelope<NotificationPreferencesResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<NotificationPreferencesResponse>>("api/auth/me/notifications");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>알림 설정 변경 — Switch의 Save 콜백용. 성공 여부만 반환(실패 시 스위치가 롤백).</summary>
        public async Task<bool> SetNotificationAsync(string itemName, bool isEnabled)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PutAsJsonAsync(
                    "api/auth/me/notifications",
                    new SetNotificationPreferenceRequest { ItemName = itemName, IsEnabled = isEnabled });

                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAccountAsync()
        {
            try
            {
                HttpResponseMessage response = await mHttp.DeleteAsync("api/auth/me");
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        //.// 이름 변경 · 로그인 수단 (Design.SettingsFlows ①②)

        /// <summary>이름 변경. 성공 시 갱신된 name 클레임의 새 토큰을 함께 돌려준다 — 호출부가 토큰을 교체해
        /// GNB·프로필을 즉시 반영한다. 실패(제한·검증)는 Success=false.</summary>
        public async Task<NameChangeResult> ChangeDisplayNameAsync(string displayName)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PutAsJsonAsync(
                    "api/auth/me/display-name", new ChangeDisplayNameRequest { DisplayName = displayName });
                Envelope<AuthResult>? envelope = await response.Content.ReadFromJsonAsync<Envelope<AuthResult>>();
                if (envelope is { IsSuccess: true, Data: not null })
                {
                    return new NameChangeResult(true, envelope.Data.AccessToken, null);
                }

                return new NameChangeResult(false, null, AppText.Errors.NameChangeInvalid);
            }
            catch
            {
                return new NameChangeResult(false, null, AppText.Errors.NameChangeRetry, IsNetworkError: true);
            }
        }

        /// <summary>로그인 수단 연결 시작 — OAuth 인가 URL을 받는다(현재 계정이 서명 상태에 실린다). null이면 실패.</summary>
        public async Task<string?> StartSocialLinkAsync(string provider)
        {
            try
            {
                Envelope<string>? envelope = await mHttp.GetFromJsonAsync<Envelope<string>>(
                    $"api/auth/social/{provider}/link");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>로그인 수단 해제 — 상태 문자열 반환 ('Ok'|'LastMeans'|'NotLinked'). 오류 시 'Error'.</summary>
        public async Task<string> UnlinkSocialAsync(string provider)
        {
            try
            {
                HttpResponseMessage response = await mHttp.DeleteAsync($"api/auth/me/social/{provider}");
                Envelope<string>? envelope = await response.Content.ReadFromJsonAsync<Envelope<string>>();
                return envelope is { IsSuccess: true } ? envelope.Data ?? "Error" : "Error";
            }
            catch
            {
                return "Error";
            }
        }
    }

    public record EmailLoginResult(bool Success, string? Token, string? Error);

    /// <summary>이름 변경 결과 — 성공 시 새 토큰(NewToken)을 교체한다. IsNetworkError로 인라인/토스트를 가른다.</summary>
    public record NameChangeResult(bool Success, string? NewToken, string? Error, bool IsNetworkError = false);
}
