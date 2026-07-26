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

        /// <summary>로스터에 선수 1명 추가. 팀 소유가 아니면 Success(null) — 거부(존재 여부 미노출).</summary>
        Task<Result<TeamRosterPlayerDto?>> AddTeamPlayerByManagerAsync(Guid managerUserId, AddTeamPlayerRequest request, CancellationToken cancellation = default);

        /// <summary>로스터에서 선수 내보내기·복구(소프트 삭제). 소유가 아니거나 대상 없음은 false.</summary>
        Task<Result<bool>> RemoveTeamPlayerByManagerAsync(Guid managerUserId, Guid teamPlayerId, bool restore, CancellationToken cancellation = default);

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

        /// <summary>팀 대시보드 일정 섹션 — 관리자 소유 팀의 일정 목록.</summary>
        Task<Result<SchedulesResponse>> GetSchedulesByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>공개 팀 홈 일정 탭 — 비공개·미존재 팀은 빈 목록.</summary>
        Task<Result<SchedulesResponse>> GetSchedulesBySlugAsync(string slug, CancellationToken cancellation = default);

        /// <summary>일정 저장 (신규·수정 겸용). 소유 아님은 Success(null).</summary>
        Task<Result<ScheduleDto?>> SaveScheduleByManagerAsync(Guid managerUserId, SaveScheduleRequest request, CancellationToken cancellation = default);

        /// <summary>일정 소프트 삭제·복구(실행취소). 소유 아님·대상 없음은 Success(false).</summary>
        Task<Result<bool>> DeleteScheduleByManagerAsync(Guid managerUserId, Guid scheduleId, bool restore, CancellationToken cancellation = default);

        /// <summary>공개 팀 홈 진학·진로 사례 조회 (Slug 기준, 비로그인). 비공개·미존재 팀은 빈 목록.</summary>
        Task<Result<TeamCareerOutcomesResponse>> GetCareerOutcomesBySlugAsync(string slug, CancellationToken cancellation = default);

        /// <summary>팀 관리자 기준 진학·진로 사례 목록.</summary>
        Task<Result<TeamCareerOutcomesResponse>> GetCareerOutcomesByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>진학·진로 사례 저장 (신규·수정 겸용). 소유 아님·미존재는 null.</summary>
        Task<Result<TeamCareerOutcomeDto?>> SaveCareerOutcomeByManagerAsync(Guid managerUserId, SaveTeamCareerOutcomeRequest request, CancellationToken cancellation = default);

        /// <summary>진학·진로 사례 소프트 삭제·복구(실행취소). 대상 없음·소유 아님은 Success(false).</summary>
        Task<Result<bool>> DeleteCareerOutcomeByManagerAsync(Guid managerUserId, Guid outcomeId, bool restore, CancellationToken cancellation = default);

        /// <summary>공개 팀 홈 리뷰 목록 + 뷰어 상태 (Slug 기준). viewerUserId = 리뷰 쓰기 자격·내 리뷰 판정용.</summary>
        Task<Result<TeamReviewsResponse>> GetReviewsBySlugAsync(string slug, Guid? viewerUserId, CancellationToken cancellation = default);

        /// <summary>리뷰 작성·수정 (재원 판정은 프로시저). 자격 없음·남의 리뷰는 null.</summary>
        Task<Result<bool>> SaveReviewAsync(Guid authorUserId, SaveTeamReviewRequest request, CancellationToken cancellation = default);

        /// <summary>리뷰 소프트 삭제·복구(실행취소) — 작성자 본인만. 대상 없음은 Success(false).</summary>
        Task<Result<bool>> DeleteReviewAsync(Guid authorUserId, Guid reviewId, bool restore, CancellationToken cancellation = default);

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

        /// <summary>보호자 기록 수정 신청 생성 — 내 자녀 관련 공식 경기만. 무권한·무관 경기는 Success(null)(사유 미노출).</summary>
        Task<Result<Guid?>> CreateGuardianCorrectionAsync(Guid userId, Guid targetPlayerId, CreateRecordCorrectionRequest request, CancellationToken cancellation = default);

        /// <summary>내가 관리하는 팀의 미처리 초대 — "처리가 필요해요" 파생 원천. 없으면 빈 목록.</summary>
        Task<Result<PendingInvitesResponse>> GetPendingInvitesByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>내가 올린 수정 신청 목록. 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<RecordCorrectionsResponse>> GetRecordCorrectionsByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>수정 신청 취소(소프트 삭제). 접수 상태가 아니거나 내 것이 아니면 Success(false).</summary>
        Task<Result<bool>> CancelRecordCorrectionAsync(Guid managerUserId, Guid correctionId, CancellationToken cancellation = default);

        //.// 선수 지원(Application) — 모집 공고 지원·검토

        /// <summary>지원 생성 (보호자). 프로시저 상태 신호를 그대로 돌려준다 —
        /// Status ∈ 'Ok'·'Duplicate'·'Closed'·'Full'·'Cooldown'·'Forbidden'. 'Ok'일 때만 ApplicationId 값.</summary>
        Task<Result<(string Status, Guid? ApplicationId)>> CreateApplicationAsync(
            Guid guardianUserId, CreateApplicationRequest request, CancellationToken cancellation = default);

        /// <summary>팀 관리자 소유 팀 공고의 지원자 목록. 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<TeamApplicationsResponse>> GetApplicationsByManagerAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>보호자가 올린 지원 현황. 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<MyApplicationsResponse>> GetApplicationsByGuardianAsync(Guid guardianUserId, CancellationToken cancellation = default);

        /// <summary>지원 상태 전환 (관리자). 소유 아님·잘못된 전환은 Success(false).</summary>
        Task<Result<bool>> UpdateApplicationStatusAsync(Guid managerUserId, Guid applicationId, string newStatus, CancellationToken cancellation = default);

        /// <summary>지원 취소(소프트 삭제, 보호자). 대기 상태의 내 지원이 아니면 Success(false).</summary>
        Task<Result<bool>> CancelApplicationAsync(Guid guardianUserId, Guid applicationId, CancellationToken cancellation = default);

        /// <summary>선수단 초대 확인 → 로스터 편입(보호자). 내 수락(Accepted) 지원이 아니면 Success(false).
        /// 편입은 SoccerTeamPlayers 삽입뿐(선수는 이미 존재)이고, 같은 팀 Active 소속이 있으면 멱등하게 건너뛴다.</summary>
        Task<Result<bool>> ConfirmApplicationInviteAsync(Guid guardianUserId, Guid applicationId, CancellationToken cancellation = default);
    }
}
