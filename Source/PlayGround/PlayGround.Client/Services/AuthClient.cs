using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Auth;
using PlayGround.Contracts.Settings;

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

                return new EmailLoginResult(false, null, envelope?.Message ?? "로그인에 실패했어요.");
            }
            catch
            {
                return new EmailLoginResult(false, null, "네트워크 오류로 로그인하지 못했어요. 잠시 후 다시 시도해 주세요.");
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

        /// <summary>계정 삭제(소프트) — 성공 시 호출부가 로그아웃 → 랜딩으로 보낸다.</summary>
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
    }

    public record EmailLoginResult(bool Success, string? Token, string? Error);
}
