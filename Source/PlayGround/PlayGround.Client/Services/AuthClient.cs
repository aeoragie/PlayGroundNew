using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Auth;

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
    }

    public record EmailLoginResult(bool Success, string? Token, string? Error);
}
