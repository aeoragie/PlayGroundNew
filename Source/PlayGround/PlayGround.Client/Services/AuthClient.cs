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
    }
}
