using Akka.Routing;
using PlayGround.Contracts.Team;

namespace PlayGround.Server.Actors
{
    /// <summary>팀 생성 메시지. ManagerUserId 해시로 사용자별 순차 처리(중복 제출 경합 방지).</summary>
    public sealed record CreateSoccerTeamMessage(Guid ManagerUserId, CreateTeamRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>팀 정보 묶음 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamInfoMessage(Guid ManagerUserId);

    /// <summary>선수단(로스터) 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamRosterMessage(Guid ManagerUserId);

    /// <summary>공개 팀 홈페이지 조회 메시지 (비로그인, Slug 기준 — RoundRobin).</summary>
    public sealed record GetSoccerTeamHomeMessage(string Slug, Guid? ViewerUserId = null);

    /// <summary>팀 탐색 공개 목록 조회 메시지 (비로그인 — RoundRobin).</summary>
    public sealed record GetSoccerTeamExploreMessage;

    /// <summary>시즌 경기 결과 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamMatchesMessage(Guid ManagerUserId, int SeasonYear);

    /// <summary>경기영상 목록 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamVideosMessage(Guid ManagerUserId);

    /// <summary>공개 팀 홈 시즌성적 조회 메시지 (비로그인, Slug 기준 — RoundRobin).</summary>
    public sealed record GetSoccerTeamSeasonRecordMessage(string Slug, int SeasonYear);

    /// <summary>공개 팀 홈 모집 탭 조회 메시지 (비로그인, Slug 기준 — RoundRobin).</summary>
    public sealed record GetSoccerTeamRecruitmentsMessage(string Slug);

    /// <summary>팀 대시보드 모집 공고 목록 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamRecruitmentsByManagerMessage(Guid ManagerUserId);

    /// <summary>모집 공고 저장 메시지 (쓰기 — ManagerUserId 해시).</summary>
    public sealed record SaveSoccerTeamRecruitmentMessage(Guid ManagerUserId, SaveTeamRecruitmentRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>모집 공고 마감 메시지 (쓰기 — ManagerUserId 해시).</summary>
    public sealed record CloseSoccerTeamRecruitmentMessage(Guid ManagerUserId, Guid RecruitmentId) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>모집 공고 삭제·복구 메시지 (쓰기 — ManagerUserId 해시).</summary>
    public sealed record DeleteSoccerTeamRecruitmentMessage(Guid ManagerUserId, Guid RecruitmentId, bool Restore) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>공개 팀 홈 진학·진로 사례 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamCareerOutcomesMessage(string Slug);

    /// <summary>팀 대시보드 진학·진로 사례 목록 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTeamCareerOutcomesByManagerMessage(Guid ManagerUserId);

    /// <summary>진학·진로 사례 저장 메시지 (쓰기 — ManagerUserId 해시).</summary>
    public sealed record SaveSoccerTeamCareerOutcomeMessage(Guid ManagerUserId, SaveTeamCareerOutcomeRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>진학·진로 사례 삭제·복구 메시지 (쓰기 — ManagerUserId 해시).</summary>
    public sealed record DeleteSoccerTeamCareerOutcomeMessage(Guid ManagerUserId, Guid OutcomeId, bool Restore) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>팀 정보 수정 메시지. ManagerUserId 해시로 순차 처리 — 가치·코치 통째 교체 경합 방지.</summary>
    public sealed record UpdateSoccerTeamInfoMessage(Guid ManagerUserId, UpdateTeamInfoRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>결과 입력 폼의 대회 선택지 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerTournamentOptionsMessage(Guid ManagerUserId, int SeasonYear);

    /// <summary>경기 결과 저장 메시지. ManagerUserId 해시로 순차 처리 — 같은 팀의 중복 제출·순위표 재계산 경합을 막는다.</summary>
    public sealed record CreateSoccerMatchResultMessage(Guid ManagerUserId, CreateTeamMatchResultRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>기록 수정 신청 생성 메시지. **중복 신청 차단이 프로시저 안에 있어 순차 처리가 중요하다** —
    /// 동시에 두 번 누르면 검사와 삽입 사이가 벌어질 수 있다(ManagerUserId 해시).</summary>
    public sealed record CreateSoccerRecordCorrectionMessage(Guid ManagerUserId, CreateRecordCorrectionRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>기록 수정 신청 취소 메시지 (쓰기 — ManagerUserId 해시).</summary>
    public sealed record CancelSoccerRecordCorrectionMessage(Guid ManagerUserId, Guid CorrectionId) : IConsistentHashable
    {
        public object ConsistentHashKey => ManagerUserId;
    }

    /// <summary>"처리가 필요해요" 항목 조회 메시지 (읽기 — RoundRobin). 현재 상태에서 파생한다.</summary>
    public sealed record GetSoccerActionItemsMessage(Guid UserId);

    /// <summary>내 기록 수정 신청 목록 조회 메시지 (읽기 — RoundRobin).</summary>
    public sealed record GetSoccerRecordCorrectionsMessage(Guid ManagerUserId);
}
