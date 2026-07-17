using System.Collections.Generic;

namespace PlayGround.Contracts.Player
{
    /// <summary>선수 온보딩 프로필 생성 요청. UserId는 본문이 아니라 인증 토큰(sub)에서 읽는다.</summary>
    public class CreatePlayerProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? BirthDate { get; set; }   // "YYYY.MM.DD" — 서버에서 파싱
        public string? AgeGroup { get; set; }     // 'U12' | 'U15' | 'U18'
        public string? Region { get; set; }
    }

    /// <summary>생성된 선수 프로필 요약.</summary>
    public class CreatePlayerProfileResponse
    {
        public Guid PlayerId { get; set; }

        /// <summary>Player로 승격된 새 액세스 토큰. 승격 실패 시 null (기존 토큰 유지).</summary>
        public string? AccessToken { get; set; }
    }

    /// <summary>선수 대시보드 프로필 묶음 (기본 카드 + 항목별 공개 설정 + 가족 계정).</summary>
    public class PlayerInfoResponse
    {
        public PlayerProfileDto Profile { get; set; } = new();

        /// <summary>5개 항목 전부 포함 — 저장값 없는 항목은 기본값(키·몸무게·주발 공개 / 학교·연락처 비공개).</summary>
        public List<PlayerFieldVisibilityDto> Visibilities { get; set; } = new();
        public List<PlayerFamilyMemberDto> Family { get; set; } = new();
    }

    /// <summary>선수 프로필. 보호자 연락처는 마스킹된 값만 내려간다 (010-****-1234).</summary>
    public class PlayerProfileDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? AgeGroup { get; set; }      // 'U12' | 'U15' | 'U18'
        public int? BirthYear { get; set; }
        public string? Grade { get; set; }         // '초4'~'고3'
        public string? Position { get; set; }      // FW | MF | DF | GK
        public string? JerseyNumber { get; set; }
        public string? TeamName { get; set; }      // 소속 없으면 null
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public string? PreferredFoot { get; set; } // SoccerPreferredFoot enum 멤버 이름 ('Left' | 'Right' | 'Both')
        public string? SchoolName { get; set; }
        public string? GuardianPhoneMasked { get; set; }
        public bool IsGuardianManaged { get; set; }
    }

    /// <summary>항목 공개 여부. FieldName은 SoccerPlayerProfileField enum 멤버 이름 문자열.</summary>
    public class PlayerFieldVisibilityDto
    {
        public string FieldName { get; set; } = string.Empty; // 'Height','Weight','PreferredFoot','School','GuardianPhone'
        public bool IsPublic { get; set; }
    }

    /// <summary>가족 구성원. Role은 'Guardian'(관리) | 'Self'(열람).</summary>
    public class PlayerFamilyMemberDto
    {
        public string MemberName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool HasAccount { get; set; }
    }

    /// <summary>항목 공개 설정 변경 요청 (보호자 = 관리 주체 계정만).</summary>
    public class SetPlayerFieldVisibilityRequest
    {
        public string FieldName { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }

    /// <summary>초대코드 Claim 요청 — 팀이 발급한 코드로 로스터 선수 프로필을 내 계정에 연결.</summary>
    public class ClaimPlayerInviteRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>Claim 결과 — 연결된 선수·팀 요약.</summary>
    public class ClaimPlayerInviteResponse
    {
        public string PlayerName { get; set; } = string.Empty;
        public string? TeamName { get; set; }

        /// <summary>Player로 승격된 새 액세스 토큰. 이미 Player였거나 승격 실패 시 null (기존 토큰 유지).</summary>
        public string? AccessToken { get; set; }
    }

    /// <summary>선수 시즌 통계 (팀 경기 결과에서 자동 집계). 요약(경기·분·득점·도움)은 클라이언트 집계.</summary>
    public class PlayerSeasonStatsResponse
    {
        public int SeasonYear { get; set; }

        /// <summary>출전 기록이 있는 연도 목록 (내림차순) — 시즌 pill.</summary>
        public List<int> SeasonYears { get; set; } = new();

        public List<PlayerMatchStatDto> Matches { get; set; } = new();
    }

    /// <summary>경기별 기록 한 행 (팀 관점 변환 완료). 경기명("vs 강동 SC (3:1 승)")은 클라이언트 조립.</summary>
    public class PlayerMatchStatDto
    {
        public Guid MatchId { get; set; }
        public DateTime? MatchedAt { get; set; }

        /// <summary>SoccerCompetitionType 멤버 이름 — 친선=대회 없음, League=리그, 그 외 Cup (서버 파생).</summary>
        public string CompetitionType { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
        public int TeamScore { get; set; }
        public int OpponentScore { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int? MinutesPlayed { get; set; }
    }

    /// <summary>커리어(소속 이력) 목록 (선수 대시보드 커리어 섹션).</summary>
    public class PlayerCareerResponse
    {
        public List<PlayerCareerEntryDto> Entries { get; set; } = new();
    }

    /// <summary>커리어 한 건. 기간 표시("2024.3 ~ 현재")는 클라이언트 포맷.</summary>
    public class PlayerCareerEntryDto
    {
        public Guid CareerId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public string? BadgeLabel { get; set; }    // 특이 뱃지 ('서울 지역 대표 선발')
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }     // null = 현재
        public string? Role { get; set; }          // 'U15 · FW · 주전'
        public string? Note { get; set; }
        public bool IsVerified { get; set; }       // 팀 확인됨 / 본인 입력
    }

    /// <summary>포트폴리오 영상 목록 (선수 대시보드 포트폴리오 섹션). 대표 영상 분리는 클라이언트에서.</summary>
    public class PlayerPortfolioResponse
    {
        public List<PlayerPortfolioVideoDto> Videos { get; set; } = new();
    }

    /// <summary>포트폴리오 영상 한 건. 길이 표시("1:42")는 클라이언트 포맷.</summary>
    public class PlayerPortfolioVideoDto
    {
        public Guid VideoId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int? DurationSeconds { get; set; }
        public bool IsPrimary { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateOnly? RecordedOn { get; set; }
    }
}
