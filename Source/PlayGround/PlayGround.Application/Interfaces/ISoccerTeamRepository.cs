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
        Task<Result<TeamPublicHomeResponse?>> GetTeamHomeBySlugAsync(string slug, Guid? viewerUserId = null, CancellationToken cancellation = default);

        /// <summary>팀 탐색 공개 목록 (핵심가치·선수단 수·올해 전적 포함). 0건은 빈 목록.</summary>
        Task<Result<TeamExploreResponse>> GetExploreTeamsAsync(CancellationToken cancellation = default);

        /// <summary>관리자 기준 시즌 종료 경기 목록(팀 관점 변환) + 리그 순위. 경기 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamMatchesResponse>> GetTeamMatchesByManagerAsync(Guid managerUserId, int seasonYear, CancellationToken cancellation = default);

        /// <summary>관리자 기준 경기영상 목록 (팀 소유 + 팀 경기 연결). 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamVideosResponse>> GetTeamVideosByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>공개 팀 홈 시즌성적(Slug 기준) — 최근 경기·리그 순위·영상. 비공개·미존재 팀은 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamSeasonRecordResponse>> GetTeamSeasonRecordBySlugAsync(string slug, int seasonYear, CancellationToken cancellation = default);

        /// <summary>팀 정보 수정 (기본 정보 + 가치 + 코치 통째 교체). 권한 없으면 Success(null).</summary>
        Task<Result<string?>> UpdateTeamInfoByManagerAsync(Guid managerUserId, UpdateTeamInfoRequest request, CancellationToken cancellation = default);

        /// <summary>결과 입력 폼의 대회/리그 선택지 (해당 시즌, 우리 팀 참가 대회 우선).</summary>
        Task<Result<TeamTournamentOptionsResponse>> GetTournamentOptionsByManagerAsync(Guid managerUserId, int seasonYear, CancellationToken cancellation = default);

        /// <summary>공개 팀 홈 모집 탭 — 비공개·미존재 팀은 빈 목록.</summary>
        Task<Result<TeamRecruitmentsResponse>> GetRecruitmentsBySlugAsync(string slug, CancellationToken cancellation = default);

        /// <summary>팀 대시보드 모집 섹션 — 관리자 소유 팀의 공고 목록.</summary>
        Task<Result<TeamRecruitmentsResponse>> GetRecruitmentsByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>모집 공고 저장 (신규·수정 겸용). 소유 아님·마감 공고 수정은 Success(null).</summary>
        Task<Result<TeamRecruitmentDto?>> SaveRecruitmentByManagerAsync(Guid managerUserId, SaveTeamRecruitmentRequest request, CancellationToken cancellation = default);

        /// <summary>모집 공고 마감 (Open → Closed 단방향). 소유 아님·이미 마감은 Success(null).</summary>
        Task<Result<TeamRecruitmentDto?>> CloseRecruitmentByManagerAsync(Guid managerUserId, Guid recruitmentId, CancellationToken cancellation = default);

        /// <summary>모집 공고 소프트 삭제·복구(실행취소). 소유 아님·대상 없음은 Success(false).</summary>
        Task<Result<bool>> DeleteRecruitmentByManagerAsync(Guid managerUserId, Guid recruitmentId, bool restore, CancellationToken cancellation = default);

        /// <summary>공개 팀 홈 진학·진로 사례 조회 (Slug 기준, 비로그인). 비공개·미존재 팀은 빈 목록.</summary>
        Task<Result<TeamCareerOutcomesResponse>> GetCareerOutcomesBySlugAsync(string slug, CancellationToken cancellation = default);

        /// <summary>팀 관리자 기준 진학·진로 사례 목록.</summary>
        Task<Result<TeamCareerOutcomesResponse>> GetCareerOutcomesByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>진학·진로 사례 저장 (신규·수정 겸용). 소유 아님·미존재는 null.</summary>
        Task<Result<TeamCareerOutcomeDto?>> SaveCareerOutcomeByManagerAsync(Guid managerUserId, SaveTeamCareerOutcomeRequest request, CancellationToken cancellation = default);

        /// <summary>진학·진로 사례 소프트 삭제·복구(실행취소). 대상 없음·소유 아님은 Success(false).</summary>
        Task<Result<bool>> DeleteCareerOutcomeByManagerAsync(Guid managerUserId, Guid outcomeId, bool restore, CancellationToken cancellation = default);

        /// <summary>
        /// 경기 결과 저장. 대회 경기면 저장 프로시저가 순위표 재계산까지 한 경로에서 수행한다(D5).
        /// 팀 미존재·없는 대회는 Success(null) — 호출자가 NotFound로 변환한다.
        /// </summary>
        Task<Result<Guid?>> CreateMatchResultByManagerAsync(Guid managerUserId, CreateTeamMatchResultRequest request, CancellationToken cancellation = default);

        /// <summary>
        /// 기록 수정 신청 생성. 남의 경기·친선경기·중복 신청은 전부 Success(null) —
        /// 프로시저가 사유를 구분하지 않고 거부하므로 호출자도 구분해 알리지 않는다.
        /// </summary>
        Task<Result<Guid?>> CreateRecordCorrectionAsync(Guid managerUserId, CreateRecordCorrectionRequest request, CancellationToken cancellation = default);

        /// <summary>내가 관리하는 팀의 미처리 초대 — "처리가 필요해요" 파생 원천. 없으면 빈 목록.</summary>
        Task<Result<PendingInvitesResponse>> GetPendingInvitesByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>내가 올린 수정 신청 목록. 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<RecordCorrectionsResponse>> GetRecordCorrectionsByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>수정 신청 취소(소프트 삭제). 접수 상태가 아니거나 내 것이 아니면 Success(false).</summary>
        Task<Result<bool>> CancelRecordCorrectionAsync(Guid managerUserId, Guid correctionId, CancellationToken cancellation = default);
    }
}
