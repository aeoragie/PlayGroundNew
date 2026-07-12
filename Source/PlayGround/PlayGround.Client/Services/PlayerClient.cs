using System.Net;
using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Player;

namespace PlayGround.Client.Services
{
    /// <summary>선수 프로필 API 호출. 인증 토큰은 공유 HttpClient 기본 헤더로 자동 부착됨.</summary>
    public class PlayerClient
    {
        private readonly HttpClient mHttp;

        public PlayerClient(HttpClient http)
        {
            mHttp = http;
        }

        public async Task<PlayerSaveResult> CreateProfileAsync(CreatePlayerProfileRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/player/me/profile", request);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new PlayerSaveResult(false, "로그인이 필요해요. 다시 로그인해 주세요.");
                }

                Envelope<CreatePlayerProfileResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<CreatePlayerProfileResponse>>();
                if (envelope is { IsSuccess: true })
                {
                    return new PlayerSaveResult(true, null);
                }

                return new PlayerSaveResult(false, envelope?.Message ?? "프로필 저장에 실패했어요.");
            }
            catch
            {
                return new PlayerSaveResult(false, "네트워크 오류로 저장하지 못했어요. 잠시 후 다시 시도해 주세요.");
            }
        }
    }

    public record PlayerSaveResult(bool Success, string? Error);
}
