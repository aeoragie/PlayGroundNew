using PlayGround.Contracts.Export;
using PlayGround.Shared.Http;
using System.Net.Http.Json;

namespace PlayGround.Client.Services
{
    /// <summary>데이터 내려받기 API 호출 (Design.SettingsFlows ③). 토큰은 공유 HttpClient 기본 헤더로 자동 부착됨.
    /// 다운로드는 서명 URL(토큰) 앵커 — DownloadUrl로 절대 경로를 조립한다(로그인 불필요).</summary>
    public class ExportClient
    {
        private readonly HttpClient mHttp;

        public ExportClient(HttpClient http)
        {
            mHttp = http;
        }

        /// <summary>현재 내려받기 상태 — 없거나 만료면 null. 진행 중이면 폴링용으로 재조회.</summary>
        public async Task<DataExportStateDto?> GetCurrentAsync()
        {
            try
            {
                Envelope<DataExportStateDto>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<DataExportStateDto>>("api/soccer/exports/me");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>내려받기 요청 접수 — Status(Ok/InProgress/Cooldown) + 현재 상태. 오류 시 null.</summary>
        public async Task<DataExportRequestResult?> RequestAsync(CreateDataExportRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/exports/me", request);
                Envelope<DataExportRequestResult>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<DataExportRequestResult>>();
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CancelAsync(Guid requestId)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsync($"api/soccer/exports/me/{requestId}/cancel", null);
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>서명 URL 다운로드 링크 (앵커 href). 토큰이 곧 자격 — 서버가 만료·횟수를 검증한다.</summary>
        public string DownloadUrl(string token) =>
            new Uri(mHttp.BaseAddress!, $"api/soccer/exports/download/{token}").ToString();
    }
}
