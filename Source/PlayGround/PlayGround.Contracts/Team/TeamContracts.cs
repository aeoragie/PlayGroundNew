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
        public DateTime? DeadlineDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
    }

    /// <summary>모집 공고 저장 요청 — RecruitmentId 빈 GUID = 신규 (B3 규약).</summary>
    public class SaveTeamRecruitmentRequest
    {
        public Guid RecruitmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
        public DateTime? DeadlineDate { get; set; }
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
        public DateTime? MatchedAt { get; set; }
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
        public DateTime MatchedAt { get; set; }

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
    }

    /// <summary>허브의 자녀 카드. 스탯은 선수 대시보드와 같은 경로로 집계한다(공식 경기만).</summary>
    public class HubChildDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AgeGroup { get; set; }
        public string? TeamName { get; set; }
        public string? Position { get; set; }
        public string? JerseyNumber { get; set; }

        public int Appearances { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
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
        public DateTime OccurredAt { get; set; }
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
        public DateTime CreatedAt { get; set; }
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

        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }

        /// <summary>경기 맥락 — "리그 12R · vs 강북 드래곤즈" 조립용.</summary>
        public string? TournamentName { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public DateTime? MatchedAt { get; set; }
    }
}
