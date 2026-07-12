using System.Collections.Generic;
using PlayGround.Contracts.Team;

namespace PlayGround.Application.Team.Models
{
    /// <summary>팀+로스터 생성 입력(검증·정규화·슬러그 완료). 유즈케이스 → 포트 전달용.</summary>
    public class CreateTeamInput
    {
        public Guid ManagerUserId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }
        public string? Region { get; set; }
        public string Slug { get; set; } = string.Empty;
        public List<RosterEntryDto> Roster { get; set; } = new();
    }
}
