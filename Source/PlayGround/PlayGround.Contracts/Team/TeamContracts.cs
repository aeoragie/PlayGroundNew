using PlayGround.Shared.Time;
using System.Collections.Generic;

namespace PlayGround.Contracts.Team
{
    /// <summary>팀 온보딩 생성 요청. ManagerUserId는 본문이 아니라 인증 토큰(sub)에서 읽는다.</summary>
    public class CreateTeamRequest
    {
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }     // 클럽 | 학교 | 학원
        public string? Region { get; set; }
        public List<RosterEntryDto> Roster { get; set; } = new();
    }

    /// <summary>로스터 한 명 (팀 소속 속성).</summary>
    public class RosterEntryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? Number { get; set; }
    }

    /// <summary>생성된 팀 요약. 완료 화면의 공개 URL·카운트 표시용.</summary>
    public class CreateTeamResponse
    {
        public string Slug { get; set; } = string.Empty;
        public int PlayerCount { get; set; }

        /// <summary>TeamAdmin으로 승격된 새 액세스 토큰. 승격 실패 시 null (기존 토큰 유지).</summary>
        public string? AccessToken { get; set; }
    }

    /// <summary>팀 정보 묶음 (대시보드 팀 정보 섹션 + 공개 홈페이지 소개 탭 공유).</summary>
    public class TeamInfoResponse
    {
        public TeamProfileDto Profile { get; set; } = new();
        public List<TeamValueDto> Values { get; set; } = new();
        public List<TeamCoachDto> Coaches { get; set; } = new();
        public List<TeamChannelDto> Channels { get; set; } = new();
    }

    /// <summary>팀 기본 정보 (기본 카드 + 사이드바 요약).</summary>
    public class TeamProfileDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }     // 클럽 | 학교 | 학원
        public string? Region { get; set; }
        public string? LogoUrl { get; set; }

        /// <summary>공개홈 히어로 커버 — 수정 폼 프리필에 쓰인다.</summary>
        public string? CoverImageUrl { get; set; }

        /// <summary>팀 소개 — 공개홈 소개 탭과 같은 값.</summary>
        public string? Description { get; set; }
        public string? Slug { get; set; }
        public bool IsVerified { get; set; }
        public int? FoundedYear { get; set; }
        public int? MonthlyFee { get; set; }      // 원
        public bool IsMonthlyFeePublic { get; set; }
        public string? TrainingDays { get; set; } // '화목금토'
    }

    /// <summary>핵심가치 한 항목.</summary>
    public class TeamValueDto
    {
        public Guid TeamValueId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>코칭스태프 한 명.</summary>
    public class TeamCoachDto
    {
        public Guid CoachId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Career { get; set; }
        public string? Certification { get; set; }
        public string? Quote { get; set; }
        public List<string> Achievements { get; set; } = new();
        public string? InstagramUrl { get; set; }
        public string? YoutubeUrl { get; set; }
    }

    /// <summary>선수단(로스터) 묶음 (대시보드 선수단 섹션).</summary>
    public class TeamRosterResponse
    {
        public List<TeamRosterPlayerDto> Players { get; set; } = new();
    }

    /// <summary>로스터에 선수 1명 추가 (대시보드 "＋ 선수 추가"). 이름만 필수 — 나머지는 나중에 채운다.
    /// 추가된 선수는 Unclaimed로 시작하고 Pending 초대코드가 함께 발급된다.</summary>
    public class AddTeamPlayerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? JerseyNumber { get; set; }
        public string? Position { get; set; }   // FW | MF | DF | GK
        public string? Grade { get; set; }       // '초4'~'고3'
        public string? AgeGroup { get; set; }    // 'U12' | 'U15' | 'U18'
    }

    /// <summary>로스터 한 명 (팀 소속 속성 + 선수 프로필 요약).</summary>
    public class TeamRosterPlayerDto
    {
        public Guid TeamPlayerId { get; set; }
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? JerseyNumber { get; set; }
        public string? Position { get; set; }   // FW | MF | DF | GK
        public string? Grade { get; set; }      // '초4'~'고3'
        public string? AgeGroup { get; set; }   // 'U12' | 'U15' | 'U18' — 온보딩 로스터는 null
        public string? PhotoUrl { get; set; }

        /// <summary>SoccerClaimStatus enum 멤버 이름 문자열. 'Claimed' | 'Unclaimed' (Pending은 Claim 플로우 도입 때).</summary>
        public string ClaimStatus { get; set; } = string.Empty;

        /// <summary>유효한 Pending 초대코드 — Unclaimed 선수만 값이 온다 (관리자 전용 API).</summary>
        public string? InviteCode { get; set; }

        /// <summary>강점 태그 (Design.StrengthTags) — 선수별 공개 설정이 꺼져 있으면 빈 목록 (SQL에서 자름).</summary>
        public List<string> StrengthTags { get; set; } = new();
    }

    /// <summary>모집 공고 목록 — 공개 홈 모집 탭·팀 대시보드 모집 섹션 공용.</summary>
    public class TeamRecruitmentsResponse
    {
        public List<TeamRecruitmentDto> Items { get; set; } = new();
    }

    /// <summary>모집 공고 한 건. IsOpen = Status 'Open' + 마감일 미경과 (서버 파생 — 화면은 그대로 렌더).</summary>
    public class TeamRecruitmentDto
    {
        public Guid RecruitmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
        /// <summary>마감 순간(UTC). 등록자의 브라우저가 "그 날의 끝"으로 변환해 보낸 값이다 —
        /// 표시는 보는 사람의 시간대로 되돌린다.</summary>
        public SystemTime? DeadlineAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsOpen { get; set; }

        /// <summary>모집 연령대 'U12'|'U15'|'U18' — 지원 통합(E5) 카드 메타. 미지정이면 null.</summary>
        public string? AgeGroup { get; set; }

        /// <summary>모집 포지션 목록 — 지원 폼의 희망 포지션 선택지 (PositionsJson 파싱).</summary>
        public List<string> Positions { get; set; } = new();

        /// <summary>정원 — null이면 무제한. "정원 N/M" 표기·지원 차단 판정에 쓴다.</summary>
        public int? Capacity { get; set; }

        /// <summary>현재 수락(Accepted)된 지원 수 — "정원 N/M"의 N. Capacity와 함께 충족 여부를 판정.</summary>
        public int AcceptedCount { get; set; }
    }

    /// <summary>모집 공고 저장 요청 — RecruitmentId 빈 GUID = 신규 (B3 규약).</summary>
    public class SaveTeamRecruitmentRequest
    {
        public Guid RecruitmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
        /// <summary>마감 순간(UTC). 클라이언트가 픽커 날짜를 "그 날의 끝"으로 변환해 보낸다 —
        /// 서버는 시간대를 모르고 `[DeadlineAt] > GETUTCDATE()`로만 판정한다.</summary>
        public SystemTime? DeadlineAt { get; set; }

        /// <summary>모집 연령대 'U12'|'U15'|'U18' (선택).</summary>
        public string? AgeGroup { get; set; }

        /// <summary>모집 포지션 목록 (선택) — 리포지토리가 JSON 배열로 직렬화해 저장한다.</summary>
        public List<string> Positions { get; set; } = new();

        /// <summary>정원 (선택) — null이면 무제한.</summary>
        public int? Capacity { get; set; }
    }

    //.// 선수 지원(Application) — 모집 공고 지원·검토 (Design.Application, E5)
    // PlayGround는 생성·조회·상태 전환·취소만 한다. 수락(Accepted)→로스터 편입·알림은 별도 단계.

    /// <summary>팀 대시보드 지원자 한 건 (관리자 뷰).</summary>
    public class ApplicationDto
    {
        public Guid ApplicationId { get; set; }
        public Guid RecruitmentId { get; set; }
        public string RecruitmentTitle { get; set; } = string.Empty;
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string? PlayerAgeGroup { get; set; }

        /// <summary>지원 선수의 소속 로스터 포지션 (있으면) — 지원의 DesiredPosition과 별개.</summary>
        public string? PlayerPosition { get; set; }
        public string? PlayerPhotoUrl { get; set; }

        /// <summary>희망 포지션 — 지원 시 선택.</summary>
        public string? DesiredPosition { get; set; }
        public string? Introduction { get; set; }

        /// <summary>SoccerApplicationStatus 멤버 이름 ('Pending'|'Reviewing'|'Accepted'|'Rejected').</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>경로 ('Direct'|'AgentRef'). AgentRef는 에이전트 서비스가 만든다(결정 4·7).</summary>
        public string Route { get; set; } = string.Empty;

        /// <summary>추천 에이전트 이름 — AgentRef일 때만 값. Direct면 null.</summary>
        public string? RefAgentName { get; set; }
        public SystemTime CreatedAt { get; set; }
    }

    /// <summary>팀 대시보드 지원자 목록 (관리자 소유 팀 공고의 지원 전부).</summary>
    public class TeamApplicationsResponse
    {
        public List<ApplicationDto> Applications { get; set; } = new();
    }

    /// <summary>보호자 지원 현황 한 건 (내가 올린 지원).</summary>
    public class MyApplicationDto
    {
        public Guid ApplicationId { get; set; }

        /// <summary>지원한 공고 — 공개홈 모집 카드의 "이미 지원함" 판정에 쓴다(제목 매칭 대신 Id로 정확히).</summary>
        public Guid RecruitmentId { get; set; }
        public string RecruitmentTitle { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string? TeamSlug { get; set; }

        /// <summary>지원한 자녀 — 허브 자녀 카드 매칭·현황 그룹핑에 쓴다(내 자녀라 노출 안전).</summary>
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string? DesiredPosition { get; set; }

        /// <summary>SoccerApplicationStatus 멤버 이름 ('Pending'|'Reviewing'|'Accepted'|'Rejected').</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>수락(Accepted) 후 보호자가 선수단 초대를 확인해 로스터에 편입됐는지 — ConfirmedAt != null.
        /// Accepted && !Confirmed = "초대를 확인해 주세요" + [초대 확인], Accepted && Confirmed = "선수단에 합류했어요".</summary>
        public bool Confirmed { get; set; }
        public SystemTime CreatedAt { get; set; }
    }

    /// <summary>보호자 지원 현황 묶음.</summary>
    public class MyApplicationsResponse
    {
        public List<MyApplicationDto> Applications { get; set; } = new();
    }

    /// <summary>지원 생성 요청 (보호자). PlayerId는 내 자녀여야 한다 — 서버가 소유를 검증한다.</summary>
    public class CreateApplicationRequest
    {
        public Guid RecruitmentId { get; set; }
        public Guid PlayerId { get; set; }
        public string? DesiredPosition { get; set; }
        public string? Introduction { get; set; }
    }

    /// <summary>지원 상태 전환 요청 (팀 관리자). Status ∈ 'Reviewing'|'Accepted'|'Rejected'.</summary>
    public class UpdateApplicationStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    //.// 팀 게시판 (Team Board, Design.TeamBoard)
    // 관리자·코치가 공지·자료를 올리고 로스터 보호자가 열람, 글 단위로 공개홈(소개 탭 "팀 소식") 노출을 선택.

    /// <summary>팀 대시보드 게시판 목록 (관리자 뷰).</summary>
    public class TeamPostsResponse
    {
        public List<TeamPostDto> Posts { get; set; } = new();
    }

    /// <summary>보호자 뷰 팀 소식 묶음 (허브 자녀 카드 → 팀 소식). TeamName은 여러 자녀 팀명 접두·헤더용.</summary>
    public class GuardianTeamPostsResponse
    {
        public string TeamName { get; set; } = string.Empty;
        public List<TeamPostDto> Posts { get; set; } = new();
    }

    /// <summary>게시판 글 한 건 (관리자·보호자 뷰). ViewCount는 관리자에게만 의미, IsRead는 보호자 뷰 안읽음 점.</summary>
    public class TeamPostDto
    {
        public Guid PostId { get; set; }

        /// <summary>SoccerTeamPostType 멤버 이름 ('Notice' | 'Material').</summary>
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public bool IsPublic { get; set; }

        /// <summary>작성자 표시명 스냅샷 — 발행 시점 이름.</summary>
        public string? AuthorName { get; set; }

        /// <summary>수정 시각 — 값이 있으면 "수정됨" 표기.</summary>
        public SystemTime? EditedAt { get; set; }
        public SystemTime CreatedAt { get; set; }

        /// <summary>조회수 (읽음 행 COUNT) — 관리자·스태프에게만 표시. 보호자 뷰에서는 무의미.</summary>
        public int ViewCount { get; set; }

        /// <summary>보호자 뷰 — 이 계정이 읽었는지 (안읽음 오렌지 점 판정). 관리자 뷰에서는 항상 false.</summary>
        public bool IsRead { get; set; }

        public List<TeamPostFileDto> Files { get; set; } = new();
    }

    /// <summary>첨부 파일 (관리자·보호자 뷰 — 다운로드 가능하므로 FileUrl 포함).</summary>
    public class TeamPostFileDto
    {
        public Guid FileId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    /// <summary>게시판 글 저장 요청 — PostId 빈 GUID = 신규 (B3 규약). 공개 스위치 기본 끔은 클라이언트 기본값.
    /// 고정(IsPinned)은 여기 없다 — 작성 폼에 없고, ⋯ "고정 전환"이 별도로 처리한다(최대 2개 제약).</summary>
    public class SaveTeamPostRequest
    {
        public Guid PostId { get; set; }

        /// <summary>SoccerTeamPostType 멤버 이름 ('Notice' | 'Material').</summary>
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsPublic { get; set; }

        /// <summary>첨부 — 업로드가 끝난 파일 목록(최대 3). 화면에 남은 전체를 보낸다(통째 교체 — 빠뜨리면 삭제).</summary>
        public List<TeamPostFileInput> Files { get; set; } = new();
    }

    /// <summary>첨부 입력 한 건 — 업로드가 끝난 공개 URL + 원본 파일명 + 크기.</summary>
    public class TeamPostFileInput
    {
        public string Url { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    /// <summary>공개 팀 홈 ① 소개 탭 "팀 소식" 섹션 (Slug 공개 조회). 유형 뱃지 없음(전부 "소식"),
    /// 첨부는 파일명만(다운로드는 로그인 필요 — 서버가 URL을 애초에 내리지 않는다).</summary>
    public class TeamNewsResponse
    {
        public List<TeamNewsDto> Items { get; set; } = new();
    }

    /// <summary>공개 소식 한 건 — 관리 정보(유형·작성자 등) 미노출.</summary>
    public class TeamNewsDto
    {
        public Guid PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public SystemTime? EditedAt { get; set; }
        public SystemTime CreatedAt { get; set; }

        /// <summary>첨부 파일명만 — 게스트는 다운로드할 수 없다(FileUrl 미포함).</summary>
        public List<TeamNewsFileDto> Files { get; set; } = new();
    }

    /// <summary>공개 소식 첨부 — 파일명·크기만 (URL 없음 = 비로그인 다운로드 차단).</summary>
    public class TeamNewsFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    //.// 팀 일정 (Schedule)

    /// <summary>일정 목록 — 공개 홈 일정 탭·팀 대시보드 일정 섹션 공용.</summary>
    public class SchedulesResponse
    {
        public List<ScheduleDto> Schedules { get; set; } = new();
    }

    /// <summary>일정 한 건. HasResult = 연결된 경기 결과(MatchId) 존재 여부 (서버 파생 — 화면은 그대로 렌더).</summary>
    public class ScheduleDto
    {
        public Guid ScheduleId { get; set; }

        /// <summary>SoccerScheduleType 멤버 이름 ('Match' | 'Tournament' | 'Training').</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>일정 제목 — 경기(Match)는 상대명에서 파생하므로 null, 대회·훈련은 값이 온다.</summary>
        public string? Title { get; set; }

        /// <summary>상대 팀 이름 — 경기·대회만 값, 훈련은 null.</summary>
        public string? OpponentName { get; set; }
        public SystemTime StartsAt { get; set; }
        public string Venue { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public Guid? MatchId { get; set; }

        /// <summary>연결된 경기 결과가 있는지 — MatchId 파생 (서버가 설정).</summary>
        public bool HasResult { get; set; }
    }

    /// <summary>일정 저장 요청 — ScheduleId 빈 GUID = 신규 (B3 규약).</summary>
    public class SaveScheduleRequest
    {
        public Guid ScheduleId { get; set; }

        /// <summary>SoccerScheduleType 멤버 이름 ('Match' | 'Tournament' | 'Training').</summary>
        public string Type { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? OpponentName { get; set; }
        public SystemTime StartsAt { get; set; }
        public string Venue { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }

    /// <summary>진학·진로 사례 목록 — 공개 홈 진학·진로 탭·팀 대시보드 관리 카드 공용.
    /// 요약 3카드는 클라이언트가 유형별 PlayerCount 합산으로 파생 (타임라인과 어긋날 수 없다).</summary>
    public class TeamCareerOutcomesResponse
    {
        public List<TeamCareerOutcomeDto> Items { get; set; } = new();
    }

    /// <summary>진학·진로 사례 한 건.</summary>
    public class TeamCareerOutcomeDto
    {
        public Guid OutcomeId { get; set; }
        public int OutcomeYear { get; set; }

        /// <summary>SoccerCareerOutcomeType 멤버 이름 문자열 ('ProTransfer' | 'SchoolTeam' | 'Promotion').</summary>
        public string OutcomeType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public int PlayerCount { get; set; }
    }

    /// <summary>진학·진로 사례 저장 요청 — OutcomeId 빈 GUID = 신규 (B3 규약).</summary>
    public class SaveTeamCareerOutcomeRequest
    {
        public Guid OutcomeId { get; set; }
        public int OutcomeYear { get; set; }
        public string OutcomeType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public int PlayerCount { get; set; } = 1;
    }

    /// <summary>진학·진로 사례 삭제·복구 요청. Restore = true면 실행취소.</summary>
    public class DeleteTeamCareerOutcomeRequest
    {
        public Guid OutcomeId { get; set; }
        public bool Restore { get; set; }
    }

    /// <summary>학부모 리뷰 목록 + 뷰어 상태 — 공개 홈 리뷰 탭. 평균·개수는 클라이언트가 목록에서 계산.</summary>
    public class TeamReviewsResponse
    {
        public List<TeamReviewDto> Items { get; set; } = new();

        /// <summary>뷰어가 이 팀 재원 자녀의 보호자인가 — 리뷰 쓰기 버튼 노출 판정.</summary>
        public bool IsResidentGuardian { get; set; }

        /// <summary>뷰어가 이미 쓴 리뷰 — 있으면 쓰기 대신 수정·삭제(⋯)로 진입.</summary>
        public Guid? MyReviewId { get; set; }
    }

    /// <summary>리뷰 한 건. 작성자 표시명·메타는 서버 파생 (이름 마스킹 "이○○ 학부모").</summary>
    public class TeamReviewDto
    {
        public Guid ReviewId { get; set; }
        public string AuthorDisplayName { get; set; } = string.Empty;

        /// <summary>"U15 · 재원 2년차" — 자녀 연령 + 재원 연차 (서버 파생, 있는 조각만).</summary>
        public string? Meta { get; set; }
        public int Rating { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>리뷰 작성·수정 요청 — ReviewId 빈 GUID = 신규 (B3 규약). 대상 팀은 공개홈 슬러그.</summary>
    public class SaveTeamReviewRequest
    {
        public Guid ReviewId { get; set; }
        public string TeamSlug { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>팀 탐색 공개 목록 (비로그인). 필터·정렬·페이징은 클라이언트 담당.</summary>
    public class TeamExploreResponse
    {
        public List<TeamExploreItemDto> Teams { get; set; } = new();
    }

    /// <summary>팀 탐색 카드 한 장 — 공개 정보만.</summary>
    public class TeamExploreItemDto
    {
        public string TeamName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? TeamType { get; set; }      // 클럽 | 학교 | 학원
        public string? Region { get; set; }
        public string? AgeGroup { get; set; }      // 'U12' | 'U15' | 'U18'
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsVerified { get; set; }
        public bool IsRecruiting { get; set; }

        /// <summary>핵심가치 제목 — 카드 teal 칩용 상위 2개.</summary>
        public List<string> Values { get; set; } = new();
        public int PlayerCount { get; set; }

        /// <summary>올해 종료된 공식 경기 전적 (승/무/패). 경기 없으면 전부 0.</summary>
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
    }

    /// <summary>공개 팀 홈페이지 묶음 (비로그인, Slug 기준). 관리 정보(Claim·UserId 등)는 포함하지 않는다.</summary>
    public class TeamPublicHomeResponse
    {
        /// <summary>열람자 = 이 팀의 관리자 본인 (GNB "관리" 텍스트 링크용 — ManagerUserId는 비노출).</summary>
        public bool IsManager { get; set; }

        public TeamPublicProfileDto Profile { get; set; } = new();
        public List<TeamValueDto> Values { get; set; } = new();
        public List<TeamCoachDto> Coaches { get; set; } = new();
        public List<TeamChannelDto> Channels { get; set; } = new();
        public List<TeamPublicPlayerDto> Roster { get; set; } = new();
    }

    /// <summary>공개 팀 프로필 (히어로 + 소개 탭). MonthlyFee는 공개 설정일 때만 값이 온다.</summary>
    public class TeamPublicProfileDto
    {
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }      // 클럽 | 학교 | 학원
        public string? Region { get; set; }
        public string? AgeGroup { get; set; }      // 팀 자체 연령 그룹 (로스터 비어 있을 때 메타 폴백)
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? Description { get; set; }
        public string? Slug { get; set; }
        public bool IsVerified { get; set; }
        public int? FoundedYear { get; set; }
        public int? MonthlyFee { get; set; }       // 원 — 비공개 설정이면 null
        public string? TrainingDays { get; set; }  // '화목금토'
    }

    /// <summary>공개 로스터 한 명 — 공개 규칙: 이름·포지션·등번호·학년·연령·사진 + 공개 프로필 여부만.</summary>
    public class TeamPublicPlayerDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? JerseyNumber { get; set; }
        public string? Position { get; set; }
        public string? Grade { get; set; }
        public string? AgeGroup { get; set; }
        public string? PhotoUrl { get; set; }

        /// <summary>공개 프로필 연결 여부 (Claimed) — "공개 프로필 →" 링크 노출용. Claim 상태 자체는 비노출.</summary>
        public bool HasPublicProfile { get; set; }

        /// <summary>공개 선수 프로필 URL 슬러그 — HasPublicProfile일 때만 값이 온다 (최소 노출).</summary>
        public string? Slug { get; set; }

        /// <summary>강점 태그 (Design.StrengthTags) — 선수별 공개 설정이 꺼져 있으면 빈 목록 (SQL에서 자름).</summary>
        public List<string> StrengthTags { get; set; } = new();
    }

    /// <summary>팀 시즌 경기 결과 묶음 (팀 대시보드 경기 결과 섹션). 시즌 요약(승무패·득실)은 클라이언트 집계.</summary>
    public class TeamMatchesResponse
    {
        public int SeasonYear { get; set; }

        /// <summary>해당 시즌 리그 순위 (League 스테이지의 우리 팀 행). 리그 미참여면 null — 카드 숨김.</summary>
        public int? LeagueRank { get; set; }

        public List<TeamMatchDto> Matches { get; set; } = new();
    }

    /// <summary>팀 관점으로 변환된 종료 경기 한 건. 승무패는 스코어에서 클라이언트 파생.</summary>
    public class TeamMatchDto
    {
        public Guid MatchId { get; set; }

        /// <summary>SoccerCompetitionType 멤버 이름 — 친선=대회 없음, League=리그 대회, 그 외 Cup (서버 파생).</summary>
        public string CompetitionType { get; set; } = string.Empty;

        /// <summary>SoccerMatchType 멤버 이름 ('Official' | 'Friendly').
        /// 집계(승무패·득실·순위표)는 Official만 — 친선은 별도 표기한다(Design.FriendlyMatch).</summary>
        public string MatchType { get; set; } = string.Empty;
        public string? TournamentName { get; set; }
        public SystemTime? MatchedAt { get; set; }
        public string? VenueName { get; set; }
        public bool IsHome { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public int TeamScore { get; set; }
        public int OpponentScore { get; set; }
        public List<TeamMatchEventDto> Events { get; set; } = new();
    }

    /// <summary>우리 팀 득점 이벤트 (칩 조립 원자료 — "득점 김민준 ×2"는 클라이언트 그룹핑).</summary>
    public class TeamMatchEventDto
    {
        public string EventType { get; set; } = string.Empty;   // 'Goal','PenaltyGoal','OwnGoal'
        public string? PlayerName { get; set; }
        public string? AssistPlayerName { get; set; }
    }

    /// <summary>팀 경기영상 목록 (팀 대시보드 경기영상 섹션).</summary>
    public class TeamVideosResponse
    {
        public List<TeamVideoDto> Videos { get; set; } = new();
    }

    /// <summary>경기영상 한 건. 길이 표시("4:12")는 클라이언트 포맷.</summary>
    public class TeamVideoDto
    {
        public Guid VideoId { get; set; }
        public string VideoType { get; set; } = string.Empty;   // SoccerVideoType 멤버 이름
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int? DurationSeconds { get; set; }
        public DateOnly? RecordedOn { get; set; }
        public bool IsMatchLinked { get; set; }                 // 메타 "경기 결과와 연결됨"
    }

    /// <summary>공개 팀 홈 시즌성적 탭 묶음 (Slug 공개 조회). 경기 카드용 팀명·시즌 요약·최근 경기·영상.
    /// 팀 대시보드 TeamMatchDto/TeamVideoDto 재사용 — 공개 뷰는 이벤트 칩 없이 승무패 뱃지만 사용.</summary>
    public class TeamSeasonRecordResponse
    {
        public string TeamName { get; set; } = string.Empty;
        public int SeasonYear { get; set; }

        /// <summary>해당 시즌 리그 순위 (League 스테이지의 우리 팀 행). 리그 미참여면 null — 카드 숨김.</summary>
        public int? LeagueRank { get; set; }

        /// <summary>최근 종료 경기 (최신순, 최대 8) — 팀 관점 변환 완료.</summary>
        public List<TeamMatchDto> Matches { get; set; } = new();
        public List<TeamVideoDto> Videos { get; set; } = new();
    }

    /// <summary>경기 결과 입력 요청 (팀 대시보드 "＋ 결과 입력").
    /// 상대 팀은 이름만 받는다 — 외부 팀이 대부분이라 TeamId를 요구할 수 없다.</summary>
    /// <remarks>
    /// 팀이 입력하는 경기는 **항상 친선경기**다 — 공식 기록의 주체는 주최측이다(설계 결정 7).
    /// 그래서 대회 선택도, 경기 성격 선택도 받지 않는다.
    /// </remarks>
    public class CreateTeamMatchResultRequest
    {
        public string OpponentName { get; set; } = string.Empty;

        /// <summary>true = 우리 팀이 홈.</summary>
        public bool IsHome { get; set; } = true;

        public int OurScore { get; set; }
        public int OpponentScore { get; set; }

        /// <summary>경기 일시 (날짜 + 시각).</summary>
        public SystemTime MatchedAt { get; set; }

        public string? VenueName { get; set; }

        /// <summary>우리 팀 득점자 (선택). 스코어와 개수가 달라도 허용 — 미상 득점이 있을 수 있다.</summary>
        public List<TeamMatchScorerDto> Scorers { get; set; } = new();
    }

    /// <summary>득점 한 건. 로스터에서 고른 선수면 PlayerId, 직접 입력이면 이름만.</summary>
    public class TeamMatchScorerDto
    {
        public Guid? PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public Guid? AssistPlayerId { get; set; }
        public string? AssistPlayerName { get; set; }
        public int? MinuteOfPlay { get; set; }
    }

    public class CreateTeamMatchResultResponse
    {
        public Guid MatchId { get; set; }
    }

    /// <summary>
    /// 팀 정보 수정 요청. 가치·코치는 통째로 교체되므로 **화면에 남아 있는 전체 목록**을 보낸다
    /// (빠뜨린 항목은 삭제된다). 이미지 URL은 업로드가 끝난 공개 경로.
    /// </summary>
    public class UpdateTeamInfoRequest
    {
        public string TeamName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Region { get; set; }
        public int? FoundedYear { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public List<TeamValueInput> Values { get; set; } = new();
        public List<TeamCoachInput> Coaches { get; set; } = new();
    }

    public class TeamValueInput
    {
        public int DisplayOrder { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TeamCoachInput
    {
        public int DisplayOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Career { get; set; }
        public string? Certification { get; set; }
        public string? Quote { get; set; }

        /// <summary>실적 칩 — DB에는 JSON 배열 문자열로 저장된다.</summary>
        public List<string> Achievements { get; set; } = new();
        public string? InstagramUrl { get; set; }
        public string? YoutubeUrl { get; set; }
    }

    /// <summary>저장 후 공개홈으로 바로 이동할 수 있도록 슬러그를 돌려준다.</summary>
    public class UpdateTeamInfoResponse
    {
        public string? Slug { get; set; }
    }

    /// <summary>결과 입력 폼의 대회/리그 선택지.</summary>
    public class TeamTournamentOptionDto
    {
        public Guid TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;   // SoccerTournamentFormat 멤버 이름
        public string? AgeGroup { get; set; }
    }

    public class TeamTournamentOptionsResponse
    {
        public List<TeamTournamentOptionDto> Tournaments { get; set; } = new();
    }

    /// <summary>공식 채널 한 개. ChannelType은 SoccerChannelType enum 멤버 이름 문자열.</summary>
    public class TeamChannelDto
    {
        public Guid ChannelId { get; set; }
        public string ChannelType { get; set; } = string.Empty; // 'YouTube' | 'Instagram'
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    //.// 대시보드 허브 (Design.DashboardHub)

    /// <summary>허브 묶음. **분기 판단의 근거이기도 하다** — 관리 대상(팀+자녀) 합이
    /// 0이면 역할 선택, 1이면 해당 대시보드로 직행, 2 이상이면 허브를 보여준다.</summary>
    public class DashboardHubResponse
    {
        public string DisplayName { get; set; } = string.Empty;

        public List<HubTeamDto> Teams { get; set; } = new();
        public List<HubChildDto> Children { get; set; } = new();

        public ActionItemsResponse Actions { get; set; } = new();

        /// <summary>팀 + 자녀. 이 수로 허브를 보여줄지 건너뛸지 정한다.</summary>
        public int ManagedCount => Teams.Count + Children.Count;
    }

    /// <summary>허브의 팀 카드.</summary>
    public class HubTeamDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public bool IsVerified { get; set; }
        public int PlayerCount { get; set; }

        /// <summary>미처리 연결 요청 — 0이면 요약 문장에서 뺀다(빈 데이터 노출 금지).</summary>
        public int PendingInviteCount { get; set; }

        /// <summary>다음 경기(경기·대회 중 가장 가까운 미래 1건, 훈련 제외) 일시 — 없으면 null(줄 생략). 클라가 포맷.</summary>
        public SystemTime? NextMatchStartsAt { get; set; }

        /// <summary>다음 경기 상대 — 있으면 "vs {상대}", 대회로 상대가 없으면 null.</summary>
        public string? NextMatchOpponent { get; set; }
    }

    /// <summary>허브의 자녀 카드. 스탯은 선수 대시보드와 같은 경로로 집계한다(공식 경기만).</summary>
    public class HubChildDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>공개 선수 프로필 슬러그 — Claimed일 때 "공개 프로필" 링크. Pending은 null.</summary>
        public string? Slug { get; set; }
        public string? AgeGroup { get; set; }
        public string? TeamName { get; set; }
        public string? Position { get; set; }
        public string? JerseyNumber { get; set; }

        public int Appearances { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }

        /// <summary>연결 상태 — 'Claimed'(연결됨) | 'Pending'(승인 대기). Pending은 스탯이 없고 "요청 상태 보기"만.</summary>
        public string ClaimStatus { get; set; } = "Claimed";

        /// <summary>이 자녀 관련 미처리(접수) 기록 수정 신청 수 — 0이면 카드에 요약 미노출.
        /// 전체 목록은 선수 대시보드 시즌 통계에 있고, 허브 카드는 요약+링크만(RecordCorrection).</summary>
        public int CorrectionPendingCount { get; set; }

        /// <summary>이 자녀의 진행 중 지원 수 — Pending·Reviewing·수락 미확인. 0이면 요약 미노출.
        /// 전체 현황은 선수 대시보드 "내 지원 현황"에 있고, 허브 카드는 요약+링크만(Design.Application §5).</summary>
        public int ApplicationPendingCount { get; set; }

        /// <summary>진행 중 지원 중 수락(Accepted)됐지만 아직 초대를 확인하지 않은 건이 있는지 — 있으면 "확인 필요"(오렌지).</summary>
        public bool ApplicationActionNeeded { get; set; }

        /// <summary>이 자녀 팀의 안읽은 게시판 글 수 (Design.TeamBoard) — 0이면 카드에 요약 미노출.
        /// 전체 목록은 팀 소식 화면에 있고, 허브 카드는 안읽음 요약+링크만.</summary>
        public int TeamNewsUnreadCount { get; set; }

        /// <summary>Pending일 때 요청일 — 대기 안내 문구에 쓴다("… 7/14 요청"). Claimed는 null.</summary>
        public SystemTime? RequestedAt { get; set; }
    }

    /// <summary>"처리가 필요해요" 목록 (Design.DashboardHub §3).
    /// **알림 테이블이 아니라 현재 상태에서 파생한다** — 읽음 상태가 없고, 처리하면 사라진다.</summary>
    public class ActionItemsResponse
    {
        /// <summary>잘라내기 전 전체 건수 — 벨 카운트가 "상위 3건"이 되면 안 된다.</summary>
        public int TotalCount { get; set; }

        /// <summary>허브에 보여줄 상위 항목(최대 3건).</summary>
        public List<ActionItemDto> Items { get; set; } = new();
    }

    /// <summary>액션 항목 한 건. 항목 전체가 딥링크라 이동 대상 Id를 함께 준다.</summary>
    public class ActionItemDto
    {
        /// <summary>SoccerActionKind 멤버 이름 ('Invite' | 'Correction') — 유형 칩 색을 정한다.</summary>
        public string Kind { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>이동 대상 — Invite는 팀 선수단, Correction은 팀 경기 결과.</summary>
        public Guid? TeamId { get; set; }
        public Guid? MatchId { get; set; }

        /// <summary>정렬 기준 (초대 발급일 / 심사일).</summary>
        public SystemTime OccurredAt { get; set; }
    }

    /// <summary>내가 관리하는 팀의 미처리 초대 목록. "처리가 필요해요"의 원천 중 하나 —
    /// 알림 테이블 없이 현재 상태에서 파생한다(생산자 없는 이벤트 로그를 만들지 않는다).</summary>
    public class PendingInvitesResponse
    {
        public List<PendingInviteDto> Invites { get; set; } = new();
    }

    /// <summary>아직 연결되지 않은 초대 한 건.</summary>
    public class PendingInviteDto
    {
        public Guid InviteId { get; set; }
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public Guid? PlayerId { get; set; }

        /// <summary>초대 대상 선수 이름 — 로스터에서 만든 미연결 프로필.</summary>
        public string? PlayerName { get; set; }
        public SystemTime CreatedAt { get; set; }
    }

    //.// 공식 기록 수정 신청 (Design.RecordCorrection)
    // PlayGround는 생성·조회·취소만 한다 — 심사·반영은 주최측(대회 운영 서비스) 몫이다(설계 결정 6·7).

    /// <summary>기록 수정 신청 요청. **1건 1항목** — 여러 오류는 신청을 여러 건 올린다.</summary>
    public class CreateRecordCorrectionRequest
    {
        public Guid MatchId { get; set; }

        /// <summary>SoccerCorrectionField 멤버 이름 ('Score' | 'GoalAssist' | 'Appearance' | 'Other').</summary>
        public string FieldType { get; set; } = string.Empty;

        /// <summary>신청 시점의 기록 — 심사 시 대조용. 화면이 자동으로 채운다.</summary>
        public string? CurrentValue { get; set; }

        public string RequestedValue { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>보호자 신청 전용 — 어느 자녀의 기록인지. 팀 관리자 경로는 null(팀 소유로 판정).</summary>
        public Guid? TargetPlayerId { get; set; }
    }

    public class RecordCorrectionsResponse
    {
        public List<RecordCorrectionDto> Corrections { get; set; } = new();
    }

    /// <summary>신청 한 건. 요약 문구("리그 12R 스코어 3:1 → 3:2")는 클라이언트 조립.</summary>
    public class RecordCorrectionDto
    {
        public Guid CorrectionId { get; set; }
        public Guid MatchId { get; set; }
        public string FieldType { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string RequestedValue { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>SoccerCorrectionStatus 멤버 이름 ('Pending' | 'Accepted' | 'Rejected').</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>반려 시 주최측이 남긴 사유 — 반려 행에는 반드시 표시한다.</summary>
        public string? RejectReason { get; set; }

        public SystemTime RequestedAt { get; set; }
        public SystemTime? ReviewedAt { get; set; }

        /// <summary>경기 맥락 — "리그 12R · vs 강북 드래곤즈" 조립용.</summary>
        public string? TournamentName { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public SystemTime? MatchedAt { get; set; }
    }
}
