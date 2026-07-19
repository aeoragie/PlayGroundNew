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

        /// <summary>본인 팀 시즌 경기 결과 조회. 미인증·오류 시 null.</summary>
        public async Task<TeamMatchesResponse?> GetTeamMatchesAsync(int seasonYear)
        {
            try
            {
                Envelope<TeamMatchesResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamMatchesResponse>>($"api/soccer/team/me/matches?season={seasonYear}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>팀 정보 저장. 성공 시 공개홈 슬러그를 돌려준다(즉시 반영 확인용).</summary>
        public async Task<TeamInfoSaveResult> UpdateTeamInfoAsync(UpdateTeamInfoRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PutAsJsonAsync("api/soccer/team/me/info", request);
                Envelope<UpdateTeamInfoResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<UpdateTeamInfoResponse>>();
                if (envelope is { IsSuccess: true })
                {
                    return new TeamInfoSaveResult(true, envelope.Data?.Slug, null);
                }

                return new TeamInfoSaveResult(false, null, envelope?.Message ?? "저장하지 못했어요. 입력을 다시 확인해 주세요.");
            }
            catch
            {
                return new TeamInfoSaveResult(false, null, "저장하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        /// <summary>결과 입력 폼의 대회/리그 선택지. 미인증·오류 시 null.</summary>
        public async Task<TeamTournamentOptionsResponse?> GetTournamentOptionsAsync(int seasonYear)
        {
            try
            {
                Envelope<TeamTournamentOptionsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamTournamentOptionsResponse>>(
                        $"api/soccer/team/me/tournament-options?season={seasonYear}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>경기 결과 저장. 성공 시 순위표는 서버에서 이미 재계산돼 있다(D5).</summary>
        public async Task<MatchResultSaveResult> CreateMatchResultAsync(CreateTeamMatchResultRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/team/me/matches", request);
                Envelope<CreateTeamMatchResultResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<CreateTeamMatchResultResponse>>();
                if (envelope is { IsSuccess: true })
                {
                    return new MatchResultSaveResult(true, null);
                }

                return new MatchResultSaveResult(false, envelope?.Message ?? "저장하지 못했어요. 입력을 다시 확인해 주세요.");
            }
            catch
            {
                return new MatchResultSaveResult(false, "저장하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        /// <summary>본인 팀 경기영상 목록 조회. 미인증·오류 시 null.</summary>
        public async Task<TeamVideosResponse?> GetTeamVideosAsync()
        {
            try
            {
                Envelope<TeamVideosResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamVideosResponse>>("api/soccer/team/me/videos");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>공개 팀 홈 시즌성적 조회 (비로그인 가능, Slug 기준). 미존재·오류 시 null.</summary>
        public async Task<TeamSeasonRecordResponse?> GetTeamSeasonRecordAsync(string slug, int seasonYear)
        {
            try
            {
                Envelope<TeamSeasonRecordResponse>? envelope = await mHttp.GetFromJsonAsync<Envelope<TeamSeasonRecordResponse>>(
                    $"api/soccer/team/{Uri.EscapeDataString(slug)}/season-record?season={seasonYear}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미존재·네트워크 오류 → null
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

    /// <remarks>
    /// IsNetworkError로 "입력이 잘못됨"(→ 인라인)과 "요청 실패"(→ 토스트+재시도)를 구분한다.
    /// </remarks>
    public record MatchResultSaveResult(bool Success, string? Error, bool IsNetworkError = false);

    /// <summary>Slug는 저장 후 "공개홈 보기"로 바로 이동하기 위한 값.</summary>
    public record TeamInfoSaveResult(bool Success, string? Slug, string? Error, bool IsNetworkError = false);
}
