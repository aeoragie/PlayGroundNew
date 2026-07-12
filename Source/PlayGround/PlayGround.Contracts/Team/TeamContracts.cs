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
    }
}
