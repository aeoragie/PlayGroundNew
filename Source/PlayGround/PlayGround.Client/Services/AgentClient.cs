using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Agent;

namespace PlayGround.Client.Services
{
    /// <summary>에이전트 열람 요청 심사 API (보호자 측). FeatureFlags.AgentApproval 뒤의 화면만 쓴다.</summary>
    public class AgentClient
    {
        private readonly HttpClient mHttp;

        public AgentClient(HttpClient http)
        {
            mHttp = http ?? throw new ArgumentNullException(nameof(http));
        }

        /// <summary>심사 화면 묶음. 소유 아님·미존재·오류는 null.</summary>
        public async Task<AgentViewRequestResponse?> GetRequestAsync(Guid requestId)
        {
            try
            {
                Envelope<AgentViewRequestResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<AgentViewRequestResponse>>(
                        $"api/soccer/agent-approvals/me/{requestId}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>심사 — Approve/Deny/Revoke. 성공 시 갱신된 요청, 실패 시 null.</summary>
        public async Task<AgentViewRequestResponse?> ReviewAsync(Guid requestId, string action)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    "api/soccer/agent-approvals/me/review",
                    new ReviewAgentViewRequestRequest { RequestId = requestId, Action = action });
                Envelope<AgentViewRequestResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<AgentViewRequestResponse>>();
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>차단 ("다시 받지 않기") — 대기 요청은 함께 거절 처리된다.</summary>
        public async Task<bool> BlockAsync(Guid requestId)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsync(
                    $"api/soccer/agent-approvals/me/{requestId}/block", null);
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }
    }
}
