using PlayGround.Shared.Time;
using PlayGround.Contracts.Soccer;

namespace PlayGround.Contracts.Player
{
    /// <summary>선수 온보딩 프로필 생성 요청. UserId는 본문이 아니라 인증 토큰(sub)에서 읽는다.</summary>
    public class CreatePlayerProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? BirthDate { get; set; }   // "YYYY.MM.DD" — 서버에서 파싱
        public SoccerAgeGroup? AgeGroup { get; set; }
        public string? Region { get; set; }
    }

    public class CreatePlayerProfileResponse
    {
        public Guid PlayerId { get; set; }

        /// <summary>Player로 승격된 새 액세스 토큰. 승격 실패 시 null (기존 토큰 유지).</summary>
        public string? AccessToken { get; set; }
    }

    /// <summary>이 계정이 관리하는 선수(자녀) 목록 — 보호자는 자녀가 여러 명일 수 있다.</summary>
    public class ManagedPlayersResponse
    {
        public List<ManagedPlayerDto> Players { get; set; } = new();
    }

    /// <summary>관리 중인 선수 한 명. 시즌 스탯은 선수 대시보드와 같은 경로로 따로 읽는다(숫자 불일치 방지).</summary>
    public class ManagedPlayerDto
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>공개 선수 프로필 슬러그(/player/{slug}) — 허브 자녀 카드의 "공개 프로필" 링크에 쓴다.</summary>
        public string? Slug { get; set; }
        public SoccerAgeGroup? AgeGroup { get; set; }
        public string? PhotoUrl { get; set; }
        public string? TeamName { get; set; }
        public string? JerseyNumber { get; set; }
        public SoccerPosition? Position { get; set; }
        public bool IsGuardianManaged { get; set; }
    }

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

        /// <summary>공개 선수 프로필(/player/{slug}) 슬러그 — "공개 프로필 보기" 링크에 쓴다.</summary>
        public string? Slug { get; set; }
        public string? PhotoUrl { get; set; }
        public SoccerAgeGroup? AgeGroup { get; set; }
        public int? BirthYear { get; set; }
        public SoccerGrade? Grade { get; set; }
        public SoccerPosition? Position { get; set; }
        public string? JerseyNumber { get; set; }
        public string? TeamName { get; set; }      // 소속 없으면 null
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public SoccerPreferredFoot? PreferredFoot { get; set; }
        public string? SchoolName { get; set; }
        public string? GuardianPhoneMasked { get; set; }
        public bool IsGuardianManaged { get; set; }

        /// <summary>사진 업로드·삭제 가능 여부 (미성년자 보호 — 보호자·팀 관리자만).
        /// false면 클라이언트는 업로드 버튼을 비활성이 아니라 아예 렌더하지 않는다.</summary>
        public bool CanEditPhoto { get; set; }

        /// <summary>강점 태그 (Design.StrengthTags) — 순서 보존. me/info는 소유자 편집 뷰라 공개 설정과 무관하게 전부 내려온다.</summary>
        public List<string> StrengthTags { get; set; } = new();
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

    public class SetPlayerFieldVisibilityRequest
    {
        public string FieldName { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }

    /// <summary>선수 프로필 수치·주소 편집 요청(키·몸무게·주발·학교 + 공개 프로필 슬러그). 폼이 전체
    /// 상태를 보내므로 수치 null은 "지움"이다. Slug null = 주소 변경 안 함.
    /// 대상 자녀는 쿼리 playerId로 지정 — 권한은 서버가 UserId로 판정.</summary>
    public class UpdatePlayerProfileInfoRequest
    {
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public SoccerPreferredFoot? PreferredFoot { get; set; }
        public string? SchoolName { get; set; }

        /// <summary>공개 프로필 주소(슬러그). null·빈 값 = 변경 안 함. 소문자 영숫자·하이픈만.</summary>
        public string? Slug { get; set; }
    }

    /// <summary>프로필 수치·주소 편집 결과. 수치는 항상 저장되며, SlugTaken이면 주소만 반영되지 않은 것
    /// (다른 선수가 이미 사용 중) — 화면은 주소 필드에 인라인 오류를 띄운다.</summary>
    public class UpdatePlayerProfileInfoResponse
    {
        public bool SlugTaken { get; set; }
    }

    /// <summary>선수 사진 설정·삭제 요청. PhotoUrl null = 삭제(이니셜 아바타로 복귀).
    /// 대상이 본인 프로필이 아닐 수 있어(팀 관리자 경로) PlayerId를 명시한다 — 권한은 서버가 판정.</summary>
    public class SetPlayerPhotoRequest
    {
        public Guid PlayerId { get; set; }
        public string? PhotoUrl { get; set; }
    }

    /// <summary>초대코드 Claim 요청 — 팀이 발급한 코드로 로스터 선수 프로필을 내 계정에 연결.</summary>
    public class ClaimPlayerInviteRequest
    {
        public string Code { get; set; } = string.Empty;
    }

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

        public List<int> SeasonYears { get; set; } = new();

        public List<PlayerMatchStatDto> Matches { get; set; } = new();
    }

    /// <summary>경기별 기록 한 행 (팀 관점 변환 완료). 경기명("vs 강동 SC (3:1 승)")은 클라이언트 조립.</summary>
    public class PlayerMatchStatDto
    {
        public Guid MatchId { get; set; }
        public SystemTime? MatchedAt { get; set; }

        /// <summary>SoccerCompetitionType 멤버 이름 — 친선=대회 없음, League=리그, 그 외 Cup (서버 파생).</summary>
        public string CompetitionType { get; set; } = string.Empty;

        /// <summary>SoccerMatchType 멤버 이름 ('Official' | 'Friendly').
        /// 시즌 요약은 Official만 집계하고 친선은 "별도"로 표기한다(Design.FriendlyMatch).</summary>
        public string MatchType { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
        public int TeamScore { get; set; }
        public int OpponentScore { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int? MinutesPlayed { get; set; }
    }

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

    /// <summary>커리어 이력 저장 요청 (신규·수정 겸용). CareerId 빈 값 = 신규.
    /// IsCurrent는 보내지 않는다 — EndDate 유무로 서버가 파생한다(모순 상태 방지).
    /// IsVerified는 팀이 다는 표시라 클라이언트가 정할 수 없다(수정하면 서버가 0으로 되돌린다).</summary>
    public class SavePlayerCareerRequest
    {
        public Guid CareerId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }   // null = 현재 소속
        public string? Role { get; set; }
        public string? Note { get; set; }
        public string? BadgeLabel { get; set; }
    }

    /// <summary>커리어 이력 삭제·복구 요청. Restore = true면 실행취소(소프트 삭제 되돌리기).</summary>
    public class DeletePlayerCareerRequest
    {
        public Guid CareerId { get; set; }
        public bool Restore { get; set; }
    }

    /// <summary>포트폴리오 영상 저장 요청 (신규·수정 겸용). VideoId 빈 값 = 신규.
    /// 첫 영상은 서버가 자동으로 대표로 만든다 — 영상이 있는데 대표가 없는 상태를 두지 않는다.</summary>
    public class SavePlayerPortfolioVideoRequest
    {
        public Guid VideoId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateOnly? RecordedOn { get; set; }
        public bool IsPrimary { get; set; }
    }

    /// <summary>포트폴리오 영상 삭제·복구 요청. Restore = true면 실행취소.</summary>
    public class DeletePlayerPortfolioVideoRequest
    {
        public Guid VideoId { get; set; }
        public bool Restore { get; set; }
    }

    /// <summary>포트폴리오 영상 목록 (선수 대시보드 포트폴리오 섹션). 대표 영상 분리는 클라이언트에서.</summary>
    public class PlayerPortfolioResponse
    {
        public List<PlayerPortfolioVideoDto> Videos { get; set; } = new();
    }

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

    /// <summary>
    /// 공개 선수 프로필 (/player/{slug} — 디테일 공개/권한 뷰).
    /// 비공개 항목은 서버에서 제외돼 응답에 실리지 않는다 (학교는 권한 뷰에만, 연락처는 어디에도 없음).
    /// </summary>
    public class PlayerPublicProfileResponse
    {
        public PlayerPublicHeaderDto Profile { get; set; } = new();

        /// <summary>시즌 요약 (공식 경기만) — 출전이 없으면 null (섹션 미노출).</summary>
        public PlayerPublicSeasonDto? Season { get; set; }

        /// <summary>대표 영상 — 없으면 null (섹션 미노출).</summary>
        public PlayerPortfolioVideoDto? PrimaryVideo { get; set; }

        public int VideoCount { get; set; }

        public List<PlayerCareerEntryDto> Careers { get; set; } = new();

        /// <summary>열람 승인 (권한 뷰) — 뷰어가 승인된 에이전트가 아니면 null.</summary>
        public PlayerPublicGrantDto? Grant { get; set; }

        /// <summary>경기별 상세 기록 (권한 뷰 전용, 친선 포함) — 권한이 없으면 null.</summary>
        public List<PlayerMatchStatDto>? Matches { get; set; }
    }

    public class PlayerPublicGrantDto
    {
        public SystemTime ApprovedAt { get; set; }
        public SystemTime ExpiresAt { get; set; }
    }

    /// <summary>공개 프로필 히어로. 키·몸무게·주발은 공개 설정이 켜진 항목만 값이 실린다.</summary>
    public class PlayerPublicHeaderDto
    {
        public string Name { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public bool IsGuardianManaged { get; set; }
        public SoccerPosition? Position { get; set; }
        public string? JerseyNumber { get; set; }
        public int? BirthYear { get; set; }
        public SoccerAgeGroup? AgeGroup { get; set; }
        public string? TeamName { get; set; }
        /// <summary>팀 공개홈 링크 — 팀 홈이 비공개면 null (링크를 걸지 않는다).</summary>
        public string? TeamSlug { get; set; }
        public bool TeamIsVerified { get; set; }

        /// <summary>미연결(UserId 없음) 선수 — 보호자가 코드 없이 "내 아이 프로필 관리하기"로 연결 요청할 수 있다.</summary>
        public bool IsClaimable { get; set; }
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public SoccerPreferredFoot? PreferredFoot { get; set; }

        /// <summary>학교 — 권한 뷰(승인된 에이전트)에만 값이 실린다. 공개 뷰는 항상 null.</summary>
        public string? SchoolName { get; set; }

        /// <summary>학년 ('초4'~'고3') — 권한 카드 "승인 열람 정보" 블록용. 공개 뷰는 항상 null.</summary>
        public SoccerGrade? Grade { get; set; }

        /// <summary>보호자 표시 이름 (마스킹 — "김OO") — 권한 카드 전용. 공개 뷰는 항상 null.</summary>
        public string? GuardianDisplayName { get; set; }

        /// <summary>강점 태그 (Design.StrengthTags) — 순서 보존. 공개 설정이 꺼져 있으면 빈 목록 (서버가 자른다).</summary>
        public List<string> StrengthTags { get; set; } = new();
    }

    /// <summary>공개 프로필 시즌 요약 — 공식 경기만 집계 (친선 미포함, Design.FriendlyMatch).</summary>
    public class PlayerPublicSeasonDto
    {
        public int SeasonYear { get; set; }
        public int MatchCount { get; set; }
        public int TotalMinutes { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        /// <summary>경기당 평균 출전(분) — 분 기록이 있는 경기 기준 (선수 대시보드와 같은 규칙).</summary>
        public int? AverageMinutes { get; set; }
    }

    /// <summary>강점 태그 프리셋 목록 (Design.StrengthTags) — 포지션별 추천 칩. 클라이언트가 포지션으로 거른다.</summary>
    public class StrengthTagPresetsResponse
    {
        public List<StrengthTagPresetDto> Presets { get; set; } = new();
    }

    /// <summary>추천 강점 태그 한 건. Position은 'GK' | 'DF' | 'MF' | 'FW'.</summary>
    public class StrengthTagPresetDto
    {
        public SoccerPosition Position { get; set; }
        public string Tag { get; set; } = string.Empty;
    }

    /// <summary>강점 태그 저장 요청 (관리 주체 = 보호자·선수 본인). 목록 전체를 통째로 보낸다(순서 보존).
    /// 개수(≤5)·길이(1~12)·금지 패턴(연락처·링크) 검증은 서버가 다시 한다.</summary>
    public class SaveStrengthTagsRequest
    {
        public List<string> Tags { get; set; } = new();
    }
}
