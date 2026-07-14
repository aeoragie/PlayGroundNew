using System.Net;
using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Team;

namespace PlayGround.Client.Services
{
    /// <summary>팀 API 호출. 인증 토큰은 공유 HttpClient 기본 헤더로 자동 부착됨.</summary>
    public class TeamClient
    {
        private readonly HttpClient mHttp;

        public TeamClient(HttpClient http)
        {
            mHttp = http;
        }

        /// <summary>본인 팀 정보 묶음 조회. 미인증·팀 없음·오류 시 null.</summary>
        public async Task<TeamInfoResponse?> GetTeamInfoAsync()
        {
            try
            {
                Envelope<TeamInfoResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamInfoResponse>>("api/soccer/team/me/info");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>본인 팀 선수단(로스터) 조회. 미인증·오류 시 null.</summary>
        public async Task<TeamRosterResponse?> GetTeamRosterAsync()
        {
            try
            {
                Envelope<TeamRosterResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamRosterResponse>>("api/soccer/team/me/roster");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>공개 팀 홈페이지 조회 (비로그인 가능). 미존재·비공개·오류 시 null.</summary>
        public async Task<TeamPublicHomeResponse?> GetTeamHomeAsync(string slug)
        {
            try
            {
                Envelope<TeamPublicHomeResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamPublicHomeResponse>>($"api/soccer/team/{Uri.EscapeDataString(slug)}/home");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미존재(404)·네트워크 오류 → null
            }
        }

        public async Task<TeamSaveResult> CreateTeamAsync(CreateTeamRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/team/me", request);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new TeamSaveResult(false, null, 0, null, "로그인이 필요해요. 다시 로그인해 주세요.");
                }

                Envelope<CreateTeamResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<CreateTeamResponse>>();
                if (envelope is { IsSuccess: true, Data: not null })
                {
                    return new TeamSaveResult(true, envelope.Data.Slug, envelope.Data.PlayerCount, envelope.Data.AccessToken, null);
                }

                return new TeamSaveResult(false, null, 0, null, envelope?.Message ?? "팀 생성에 실패했어요.");
            }
            catch
            {
                return new TeamSaveResult(false, null, 0, null, "네트워크 오류로 저장하지 못했어요. 잠시 후 다시 시도해 주세요.");
            }
        }
    }

    /// <summary>AccessToken은 TeamAdmin으로 승격된 새 토큰 — null이면 기존 토큰 유지.</summary>
    public record TeamSaveResult(bool Success, string? Slug, int PlayerCount, string? AccessToken, string? Error);
}
