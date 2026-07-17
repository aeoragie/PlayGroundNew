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

        /// <summary>본인(관리 주체) 선수 프로필 묶음 조회. 미인증·미존재·오류 시 null.</summary>
        public async Task<PlayerInfoResponse?> GetMyInfoAsync()
        {
            try
            {
                Envelope<PlayerInfoResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerInfoResponse>>("api/soccer/player/me/info");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>본인(관리 주체) 커리어 목록 조회. 미인증·오류 시 null.</summary>
        public async Task<PlayerCareerResponse?> GetMyCareerAsync()
        {
            try
            {
                Envelope<PlayerCareerResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerCareerResponse>>("api/soccer/player/me/career");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>본인(관리 주체) 포트폴리오 영상 목록 조회. 미인증·오류 시 null.</summary>
        public async Task<PlayerPortfolioResponse?> GetMyPortfolioAsync()
        {
            try
            {
                Envelope<PlayerPortfolioResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerPortfolioResponse>>("api/soccer/player/me/portfolio");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>본인(관리 주체) 시즌 통계 조회. 미인증·오류 시 null.</summary>
        public async Task<PlayerSeasonStatsResponse?> GetMySeasonStatsAsync(int seasonYear)
        {
            try
            {
                Envelope<PlayerSeasonStatsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PlayerSeasonStatsResponse>>($"api/soccer/player/me/season-stats?season={seasonYear}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null; // 미인증(401)·네트워크 오류 → null
            }
        }

        /// <summary>항목 공개 설정 변경. 성공 여부 반환.</summary>
        public async Task<bool> SetFieldVisibilityAsync(string fieldName, bool isPublic)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PutAsJsonAsync(
                    "api/soccer/player/me/profile/visibility",
                    new SetPlayerFieldVisibilityRequest { FieldName = fieldName, IsPublic = isPublic });
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
                return new PlayerClaimResult(false, null, null, "네트워크 오류로 연결하지 못했어요. 잠시 후 다시 시도해 주세요.");
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

    /// <summary>초대코드 Claim 결과. AccessToken은 승격 시에만 값이 온다.</summary>
    public record PlayerClaimResult(bool Success, string? TeamName, string? AccessToken, string? Error);
}
