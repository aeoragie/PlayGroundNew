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

    /// <summary>공개 팀 홈페이지 묶음 (비로그인, Slug 기준). 관리 정보(Claim·UserId 등)는 포함하지 않는다.</summary>
    public class TeamPublicHomeResponse
    {
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
    public class CreateTeamMatchResultRequest
    {
        /// <summary>참가 대회. null이면 친선 경기.</summary>
        public Guid? TournamentId { get; set; }

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
}
