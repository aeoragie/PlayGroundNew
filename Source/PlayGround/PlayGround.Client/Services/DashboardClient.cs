using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Team;

namespace PlayGround.Client.Services
{
    /// <summary>대시보드 허브 API. 팀·자녀·액션을 한 번에 받는다 — 라우팅 분기의 근거이기도 하다.</summary>
    public class DashboardClient
    {
        private readonly HttpClient mHttp;

        public DashboardClient(HttpClient http)
        {
            mHttp = http ?? throw new ArgumentNullException(nameof(http));
        }

        /// <summary>허브 묶음. 오류 시 null — 호출부가 실패와 "관리 대상 0개"를 구분해야 한다.</summary>
        public async Task<DashboardHubResponse?> GetHubAsync()
        {
            try
            {
                Envelope<DashboardHubResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<DashboardHubResponse>>("api/soccer/dashboard/me/hub");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }
    }
}
