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

        /// <summary>자녀 지정 쿼리 — 주지 않으면 서버가 첫 자녀를 쓴다.
        /// first=false면 이미 쿼리가 있는 URL이라 ?가 아니라 &amp;로 잇는다.</summary>
        private static string Q(Guid? playerId, bool first = true) =>
            playerId is null ? string.Empty : $"{(first ? '?' : '&')}playerId={playerId}";

        /// <summary>이 계정이 관리하는 선수(자녀) 목록. 보호자는 여러 명일 수 있다. 미인증·오류 시 null.</summary>
        public async Task<ManagedPlayersResponse?> GetMyPlayersAsync()
        {
            try
            {
                Envelope<ManagedPlayersResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<ManagedPlayersResponse>>(
                        "api/soccer/player/me/players");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>본인(관리 주체) 선수 프로필 묶음 조회. 미인증·미존재·오류 시 null.</summary>
        public async Task<PlayerInfoResponse?> GetMyInfoAsync(Guid? playerId = null)
        {
            try
            {
                Envelope<PlayerInfoResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerInfoResponse>>($"api/soccer/player/me/info{Q(playerId)}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>본인(관리 주체) 커리어 목록 조회. 미인증·오류 시 null.</summary>
        public async Task<PlayerCareerResponse?> GetMyCareerAsync(Guid? playerId = null)
        {
            try
            {
                Envelope<PlayerCareerResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerCareerResponse>>($"api/soccer/player/me/career{Q(playerId)}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>본인(관리 주체) 포트폴리오 영상 목록 조회. 미인증·오류 시 null.</summary>
        public async Task<PlayerPortfolioResponse?> GetMyPortfolioAsync(Guid? playerId = null)
        {
            try
            {
                Envelope<PlayerPortfolioResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerPortfolioResponse>>($"api/soccer/player/me/portfolio{Q(playerId)}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>본인(관리 주체) 시즌 통계 조회. 미인증·오류 시 null.</summary>
        public async Task<PlayerSeasonStatsResponse?> GetMySeasonStatsAsync(int seasonYear, Guid? playerId = null)
        {
            try
            {
                Envelope<PlayerSeasonStatsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerSeasonStatsResponse>>($"api/soccer/player/me/season-stats?season={seasonYear}{Q(playerId, first: false)}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>공개 선수 프로필 조회 (비로그인 가능). 미존재·프로필 비공개·오류 시 null.</summary>
        public async Task<PlayerPublicProfileResponse?> GetPublicProfileAsync(string slug, int seasonYear)
        {
            try
            {
                Envelope<PlayerPublicProfileResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerPublicProfileResponse>>(
                        $"api/soccer/player/{Uri.EscapeDataString(slug)}/profile?season={seasonYear}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미존재(404)·네트워크 오류 → null
            }
        }

        /// <summary>항목 공개 설정 변경. 성공 여부 반환.</summary>
        public async Task<bool> SetFieldVisibilityAsync(string fieldName, bool isPublic, Guid? playerId = null)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PutAsJsonAsync(
                     $"api/soccer/player/me/profile/visibility{Q(playerId)}",
                    new SetPlayerFieldVisibilityRequest { FieldName = fieldName, IsPublic = isPublic });
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>커리어 이력 저장(신규·수정). CareerId 빈 값 = 신규.</summary>
        public Task<PlayerEntrySaveResult> SaveCareerAsync(SavePlayerCareerRequest request, Guid? playerId = null) =>
            PutAsync($"api/soccer/player/me/career{Q(playerId)}", request);

        /// <summary>커리어 이력 삭제·복구(실행취소).</summary>
        public Task<PlayerEntrySaveResult> DeleteCareerAsync(Guid careerId, bool restore = false, Guid? playerId = null) =>
            PostAsync($"api/soccer/player/me/career/delete{Q(playerId)}",
                new DeletePlayerCareerRequest { CareerId = careerId, Restore = restore });

        /// <summary>포트폴리오 영상 저장(신규·수정). VideoId 빈 값 = 신규.</summary>
        public Task<PlayerEntrySaveResult> SavePortfolioVideoAsync(SavePlayerPortfolioVideoRequest request, Guid? playerId = null) =>
            PutAsync($"api/soccer/player/me/portfolio{Q(playerId)}", request);

        /// <summary>포트폴리오 영상 삭제·복구(실행취소).</summary>
        public Task<PlayerEntrySaveResult> DeletePortfolioVideoAsync(Guid videoId, bool restore = false, Guid? playerId = null) =>
            PostAsync($"api/soccer/player/me/portfolio/delete{Q(playerId)}",
                new DeletePlayerPortfolioVideoRequest { VideoId = videoId, Restore = restore });

        // 저장 계열은 응답 형태가 같다 — 실패 사유(입력 거부 vs 요청 실패)를 구분해 돌려준다
        private async Task<PlayerEntrySaveResult> PutAsync<TRequest>(string url, TRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PutAsJsonAsync(url, request);
                return await ReadSaveResultAsync(response);
            }
            catch
            {
                return new PlayerEntrySaveResult(false, "저장하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        private async Task<PlayerEntrySaveResult> PostAsync<TRequest>(string url, TRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(url, request);
                return await ReadSaveResultAsync(response);
            }
            catch
            {
                return new PlayerEntrySaveResult(false, "저장하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        private static async Task<PlayerEntrySaveResult> ReadSaveResultAsync(HttpResponseMessage response)
        {
            Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
            if (envelope is { IsSuccess: true })
            {
                return new PlayerEntrySaveResult(true, null);
            }

            // Envelope.Message는 영어 진단 문구다(로그용) — 사용자에게는 우리 문장을 보여준다.
            // 여기까지 온 입력 오류는 클라이언트 검증이 놓친 경우라 항목을 특정할 수 없다.
            return new PlayerEntrySaveResult(false, "저장하지 못했어요. 입력을 다시 확인해 주세요.");
        }

        /// <summary>선수 사진 설정·삭제(photoUrl null = 삭제). 권한은 서버가 판정 — 거부되면 false.</summary>
        public async Task<bool> SetPlayerPhotoAsync(Guid playerId, string? photoUrl)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PutAsJsonAsync(
                    "api/soccer/player/photo",
                    new SetPlayerPhotoRequest { PlayerId = playerId, PhotoUrl = photoUrl });
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>초대코드 Claim — 성공 시 연결된 팀 이름과 (승격 시) 새 토큰.</summary>
        public async Task<PlayerClaimResult> ClaimInviteAsync(string code)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    "api/soccer/player/me/claim", new ClaimPlayerInviteRequest { Code = code });
                Envelope<ClaimPlayerInviteResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<ClaimPlayerInviteResponse>>();
                if (envelope is { IsSuccess: true, Data: not null })
                {
                    return new PlayerClaimResult(true, envelope.Data.TeamName, envelope.Data.AccessToken, null);
                }

                return new PlayerClaimResult(false, null, null, "코드가 유효하지 않아요. 팀 관리자에게 다시 확인해 주세요.");
            }
            catch
            {
                return new PlayerClaimResult(false, null, null, "연결하지 못했어요. 잠시 후 다시 시도해 주세요.", IsNetworkError: true);
            }
        }

        public async Task<PlayerSaveResult> CreateProfileAsync(CreatePlayerProfileRequest request)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync("api/soccer/player/me/profile", request);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new PlayerSaveResult(false, null, "로그인이 필요해요. 다시 로그인해 주세요.");
                }

                Envelope<CreatePlayerProfileResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<Envelope<CreatePlayerProfileResponse>>();
                if (envelope is { IsSuccess: true })
                {
                    return new PlayerSaveResult(true, envelope.Data?.AccessToken, null);
                }

                return new PlayerSaveResult(false, null, envelope?.Message ?? "프로필 저장에 실패했어요.");
            }
            catch
            {
                return new PlayerSaveResult(false, null, "네트워크 오류로 저장하지 못했어요. 잠시 후 다시 시도해 주세요.");
            }
        }
    }

    /// <summary>AccessToken은 Player로 승격된 새 토큰 — null이면 기존 토큰 유지.</summary>
    public record PlayerSaveResult(bool Success, string? AccessToken, string? Error);

    /// <summary>커리어·포트폴리오 항목 저장·삭제 결과.</summary>
    /// <remarks>
    /// IsNetworkError로 "입력이 잘못됨"(→ 인라인)과 "요청 실패"(→ 토스트+재시도)를 구분한다.
    /// </remarks>
    public record PlayerEntrySaveResult(bool Success, string? Error, bool IsNetworkError = false);

    /// <summary>초대코드 Claim 결과. AccessToken은 승격 시에만 값이 온다.</summary>
    /// <remarks>
    /// IsNetworkError로 "코드가 틀림"(입력 오류 → 인라인)과 "요청 실패"(시스템 오류 → 토스트+재시도)를 구분한다.
    /// </remarks>
    public record PlayerClaimResult(bool Success, string? TeamName, string? AccessToken, string? Error, bool IsNetworkError = false);
}
