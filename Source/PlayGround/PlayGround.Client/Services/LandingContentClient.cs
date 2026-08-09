using PlayGround.Contracts.Landing;
using PlayGround.Shared.Http;
using System.Net.Http.Json;

namespace PlayGround.Client.Services
{
    /// <summary>랜딩 콘텐츠 API 호출. 실패 시 null 반환(호출부가 기본 카피로 폴백).</summary>
    public class LandingContentClient
    {
        private readonly HttpClient mHttp;

        public LandingContentClient(HttpClient http)
        {
            mHttp = http;
        }

        public async Task<LandingContentsResponse?> GetContentsAsync()
        {
            try
            {
                var envelope = await mHttp.GetFromJsonAsync<Envelope<LandingContentsResponse>>("api/soccer/landing/contents");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
