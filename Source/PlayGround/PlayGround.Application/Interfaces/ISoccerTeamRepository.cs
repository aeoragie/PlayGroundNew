using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Team.Models;

namespace PlayGround.Application.Interfaces
{
    /// <summary>팀 저장·조회 포트 (Persistence에서 구현). 생성 시 최종 슬러그 반환.</summary>
    public interface ISoccerTeamRepository
    {
        Task<Result<string>> CreateWithRosterAsync(CreateTeamInput input, CancellationToken cancellation = default);

        /// <summary>관리자 기준 팀 정보 묶음 조회. 팀 미존재 시 Success(null) — 에러가 아니다.</summary>
        Task<Result<TeamInfoResponse?>> GetTeamInfoByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>관리자 기준 선수단(로스터) 조회. 팀 미존재·로스터 없음은 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamRosterResponse>> GetTeamRosterByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>공개 팀 홈페이지 묶음 조회 (Slug 기준). 미존재·비공개 팀은 Success(null) — 에러가 아니다.</summary>
        Task<Result<TeamPublicHomeResponse?>> GetTeamHomeBySlugAsync(string slug, CancellationToken cancellation = default);

        /// <summary>관리자 기준 시즌 종료 경기 목록(팀 관점 변환) + 리그 순위. 경기 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamMatchesResponse>> GetTeamMatchesByManagerAsync(Guid managerUserId, int seasonYear, CancellationToken cancellation = default);

        /// <summary>관리자 기준 경기영상 목록 (팀 소유 + 팀 경기 연결). 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamVideosResponse>> GetTeamVideosByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>공개 팀 홈 시즌성적(Slug 기준) — 최근 경기·리그 순위·영상. 비공개·미존재 팀은 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamSeasonRecordResponse>> GetTeamSeasonRecordBySlugAsync(string slug, int seasonYear, CancellationToken cancellation = default);
    }
}
