using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Records;

namespace PlayGround.Client.Services
{
    /// <summary>공개 경기기록(Records) API 호출 — 비로그인 접근 가능.</summary>
    public class RecordsClient
    {
        private readonly HttpClient mHttp;

        public RecordsClient(HttpClient http)
        {
            mHttp = http;
        }

        /// <summary>시즌 대회/리그 목록 + 우승팀 + 기록 연도 목록. 오류 시 null.</summary>
        public async Task<RecordsTournamentsResponse?> GetTournamentsAsync(int seasonYear)
        {
            try
            {
                Envelope<RecordsTournamentsResponse>? envelope = await mHttp.GetFromJsonAsync<Envelope<RecordsTournamentsResponse>>(
                    $"api/soccer/records/tournaments?season={seasonYear}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 네트워크 오류 → null
            }
        }
    }
}
