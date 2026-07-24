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

        //.// 모집 공고 (Design.TeamPublicHome ④ 모집)

        /// <summary>공개 팀 홈 모집 탭 — 비로그인. 비공개·미존재 팀은 빈 목록, 오류 시 null.</summary>
        public async Task<TeamRecruitmentsResponse?> GetTeamRecruitmentsAsync(string slug)
        {
            try
            {
                Envelope<TeamRecruitmentsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamRecruitmentsResponse>>(
                        $"api/soccer/team/{Uri.EscapeDataString(slug)}/recruitments");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>팀 대시보드 모집 섹션 — 소유 팀 공고 목록. 오류 시 null.</summary>
        public async Task<TeamRecruitmentsResponse?> GetMyRecruitmentsAsync()
        {
            try
            {
                Envelope<TeamRecruitmentsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamRecruitmentsResponse>>("api/soccer/team/me/recruitments");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>로스터에 선수 1명 추가 (＋ 선수 추가). 성공 후 호출부가 로스터를 다시 읽는다.</summary>
        public async Task<PlayerAddResult> AddPlayerAsync(AddTeamPlayerRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/team/me/roster/players", request);
                Envelope<TeamRosterPlayerDto>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<TeamRosterPlayerDto>>();
                if (envelope is { IsSuccess: true })
                {
                    return new PlayerAddResult(true, null);
                }

                return new PlayerAddResult(false, "선수를 추가하지 못했어요. 입력을 다시 확인해 주세요.");
            }
            catch
            {
                return new PlayerAddResult(false, "선수를 추가하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        /// <summary>로스터에서 선수 내보내기·복구(restore = 실행취소). 성공 여부만.</summary>
        public async Task<bool> RemovePlayerAsync(Guid teamPlayerId, bool restore = false)
        {
            try
            {
                HttpResponseMessage response = await mHttp.DeleteAsync(
                    $"api/soccer/team/me/roster/players/{teamPlayerId}?restore={(restore ? "true" : "false")}");
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>모집 공고 저장 (신규·수정 겸용).</summary>
        public async Task<RecruitmentSaveResult> SaveRecruitmentAsync(SaveTeamRecruitmentRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/team/me/recruitments", request);
                Envelope<TeamRecruitmentDto>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<TeamRecruitmentDto>>();
                if (envelope is { IsSuccess: true })
                {
                    return new RecruitmentSaveResult(true, null);
                }

                return new RecruitmentSaveResult(false, "저장하지 못했어요. 입력을 다시 확인해 주세요.");
            }
            catch
            {
                return new RecruitmentSaveResult(false, "저장하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        /// <summary>모집 공고 마감 — 성공 여부만 (실패는 호출부가 토스트).</summary>
        public async Task<bool> CloseRecruitmentAsync(Guid recruitmentId)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsync(
                    $"api/soccer/team/me/recruitments/{recruitmentId}/close", null);
                Envelope<TeamRecruitmentDto>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<TeamRecruitmentDto>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>모집 공고 삭제·복구(restore = 실행취소).</summary>
        public async Task<bool> DeleteRecruitmentAsync(Guid recruitmentId, bool restore = false)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsync(
                    $"api/soccer/team/me/recruitments/{recruitmentId}/delete?restore={(restore ? "true" : "false")}", null);
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>공개 팀 홈 진학·진로 탭 (비로그인 가능). 미존재·오류 시 null.</summary>
        public async Task<TeamCareerOutcomesResponse?> GetTeamCareerOutcomesAsync(string slug)
        {
            try
            {
                Envelope<TeamCareerOutcomesResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamCareerOutcomesResponse>>(
                        $"api/soccer/team/{Uri.EscapeDataString(slug)}/career-outcomes");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>팀 대시보드 진학·진로 관리 카드 — 소유 팀 사례 목록. 오류 시 null.</summary>
        public async Task<TeamCareerOutcomesResponse?> GetMyCareerOutcomesAsync()
        {
            try
            {
                Envelope<TeamCareerOutcomesResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamCareerOutcomesResponse>>("api/soccer/team/me/career-outcomes");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>진학·진로 사례 저장 (신규·수정 겸용).</summary>
        public async Task<RecruitmentSaveResult> SaveCareerOutcomeAsync(SaveTeamCareerOutcomeRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/team/me/career-outcomes", request);
                Envelope<TeamCareerOutcomeDto>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<TeamCareerOutcomeDto>>();
                if (envelope is { IsSuccess: true })
                {
                    return new RecruitmentSaveResult(true, null);
                }

                return new RecruitmentSaveResult(false, "저장하지 못했어요. 입력을 다시 확인해 주세요.");
            }
            catch
            {
                return new RecruitmentSaveResult(false, "저장하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        /// <summary>진학·진로 사례 삭제·복구(restore = 실행취소).</summary>
        public async Task<bool> DeleteCareerOutcomeAsync(Guid outcomeId, bool restore = false)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsync(
                    $"api/soccer/team/me/career-outcomes/{outcomeId}/delete?restore={(restore ? "true" : "false")}", null);
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>공개 팀 홈 리뷰 탭 (비로그인 가능 — 로그인 시 쓰기 자격·내 리뷰 판정 포함). 오류 시 null.</summary>
        public async Task<TeamReviewsResponse?> GetTeamReviewsAsync(string slug)
        {
            try
            {
                Envelope<TeamReviewsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamReviewsResponse>>(
                        $"api/soccer/team/{Uri.EscapeDataString(slug)}/reviews");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>리뷰 작성·수정 (재원 확인 보호자만 — 판정은 서버).</summary>
        public async Task<RecruitmentSaveResult> SaveReviewAsync(SaveTeamReviewRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    $"api/soccer/team/{Uri.EscapeDataString(request.TeamSlug)}/reviews", request);
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                if (envelope is { IsSuccess: true })
                {
                    return new RecruitmentSaveResult(true, null);
                }

                return new RecruitmentSaveResult(false, "리뷰를 저장하지 못했어요. 입력을 다시 확인해 주세요.");
            }
            catch
            {
                return new RecruitmentSaveResult(false, "리뷰를 저장하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        /// <summary>리뷰 삭제·복구(restore = 실행취소) — 작성자 본인만.</summary>
        public async Task<bool> DeleteReviewAsync(Guid reviewId, bool restore = false)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsync(
                    $"api/soccer/team/reviews/{reviewId}/delete?restore={(restore ? "true" : "false")}", null);
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>"처리가 필요해요" 항목 (허브). 현재 상태에서 파생 — 읽음 상태가 없다. 오류 시 null.</summary>
        public async Task<ActionItemsResponse?> GetActionItemsAsync()
        {
            try
            {
                Envelope<ActionItemsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<ActionItemsResponse>>("api/soccer/team/me/action-items");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>기록 수정 신청 목록 조회. 미인증·오류 시 null.</summary>
        public async Task<RecordCorrectionsResponse?> GetRecordCorrectionsAsync()
        {
            try
            {
                Envelope<RecordCorrectionsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<RecordCorrectionsResponse>>("api/soccer/team/me/corrections");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>기록 수정 신청 생성. 거부 사유는 서버가 구분해 주지 않는다(남의 경기·친선·중복).</summary>
        public async Task<CorrectionSaveResult> CreateRecordCorrectionAsync(CreateRecordCorrectionRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/team/me/corrections", request);
                Envelope<Guid>? envelope = await response.Content.ReadFromJsonAsync<Envelope<Guid>>();
                if (envelope is { IsSuccess: true })
                {
                    return new CorrectionSaveResult(true, null);
                }

                // Envelope.Message는 영어 진단 문구라 사용자에게 보여주지 않는다
                return new CorrectionSaveResult(false, "신청하지 못했어요. 이미 처리 중인 신청이 있는지 확인해 주세요.");
            }
            catch
            {
                return new CorrectionSaveResult(false, "신청하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        /// <summary>기록 수정 신청 취소 — 접수 상태만 가능.</summary>
        public async Task<CorrectionSaveResult> CancelRecordCorrectionAsync(Guid correctionId)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    $"api/soccer/team/me/corrections/{correctionId}/cancel", new { });
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                if (envelope is { IsSuccess: true })
                {
                    return new CorrectionSaveResult(true, null);
                }

                return new CorrectionSaveResult(false, "취소하지 못했어요. 이미 심사가 시작됐을 수 있어요.");
            }
            catch
            {
                return new CorrectionSaveResult(false, "취소하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
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

        /// <summary>팀 탐색 공개 목록 (비로그인 가능). 오류 시 null — 페이지가 실패 상태로 구분한다.</summary>
        public async Task<TeamExploreResponse?> GetExploreTeamsAsync()
        {
            try
            {
                Envelope<TeamExploreResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<TeamExploreResponse>>("api/soccer/teams");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 네트워크 오류 → null (LoadingGate 실패 처리)
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

    /// <summary>기록 수정 신청 결과. 거부 사유(남의 경기·친선·중복)는 서버가 구분해 주지 않는다.</summary>
    public record CorrectionSaveResult(bool Success, string? Error, bool IsNetworkError = false);

    /// <summary>Slug는 저장 후 "공개홈 보기"로 바로 이동하기 위한 값.</summary>
    public record TeamInfoSaveResult(bool Success, string? Slug, string? Error, bool IsNetworkError = false);

    /// <summary>모집 공고 저장 결과 — 입력 거부(인라인)와 요청 실패(토스트)를 IsNetworkError로 가른다.</summary>
    public record RecruitmentSaveResult(bool Success, string? Error, bool IsNetworkError = false);

    /// <summary>선수 추가 결과 — 입력 거부(인라인)와 요청 실패(토스트)를 IsNetworkError로 가른다.</summary>
    public record PlayerAddResult(bool Success, string? Error, bool IsNetworkError = false);
}
