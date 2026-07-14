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
