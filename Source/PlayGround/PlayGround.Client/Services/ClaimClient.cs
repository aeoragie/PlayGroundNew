using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Claim;

namespace PlayGround.Client.Services
{
    /// <summary>Claim 플로우 API — /claim 4스텝 + 팀 관리자 승인/거절.
    /// 코드 오류(인라인)와 요청 실패(토스트+재시도)를 IsNetworkError로 가른다.</summary>
    public class ClaimClient
    {
        private readonly HttpClient mHttp;

        public ClaimClient(HttpClient http)
        {
            mHttp = http ?? throw new ArgumentNullException(nameof(http));
        }

        /// <summary>스텝 ①→②: 코드로 선수 카드 조회.</summary>
        public async Task<ClaimLookupResult> LookupAsync(string code)
        {
            try
            {
                Envelope<ClaimInviteCardResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<ClaimInviteCardResponse>>(
                        $"api/soccer/claim/invite/{Uri.EscapeDataString(code)}");
                return envelope is { IsSuccess: true, Data: not null }
                    ? new ClaimLookupResult(envelope.Data, false)
                    : new ClaimLookupResult(null, false);
            }
            catch
            {
                return new ClaimLookupResult(null, true);
            }
        }

        /// <summary>스텝 ②→③: 연결 요청 생성.</summary>
        public async Task<ClaimCreateResult> CreateRequestAsync(string code, string relation)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    "api/soccer/claim/me/requests", new CreateClaimRequestRequest { Code = code, Relation = relation });
                Envelope<ClaimRequestSummaryResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<ClaimRequestSummaryResponse>>();
                return envelope is { IsSuccess: true, Data: not null }
                    ? new ClaimCreateResult(envelope.Data, false)
                    : new ClaimCreateResult(null, false);
            }
            catch
            {
                return new ClaimCreateResult(null, true);
            }
        }

        /// <summary>재방문 복원 — 내 최신 요청. 없거나 실패면 null.</summary>
        public async Task<ClaimRequestSummaryResponse?> GetMyRequestAsync()
        {
            try
            {
                Envelope<ClaimRequestSummaryResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<ClaimRequestSummaryResponse>>("api/soccer/claim/me/request");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>승인/거절 — 팀 관리자. 실패 시 null.</summary>
        public async Task<ReviewClaimResponse?> ReviewAsync(Guid requestId, bool approve)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    "api/soccer/claim/requests/review", new ReviewClaimRequestRequest { RequestId = requestId, Approve = approve });
                Envelope<ReviewClaimResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<ReviewClaimResponse>>();
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public record ClaimLookupResult(ClaimInviteCardResponse? Card, bool IsNetworkError);

    public record ClaimCreateResult(ClaimRequestSummaryResponse? Request, bool IsNetworkError);
}
