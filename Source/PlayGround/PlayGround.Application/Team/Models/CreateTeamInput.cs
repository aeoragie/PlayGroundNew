using System.Collections.Generic;
using PlayGround.Contracts.Team;

namespace PlayGround.Application.Team.Models
{
    /// <summary>팀+로스터 생성 입력(검증·정규화 완료). 유즈케이스 → 포트 전달용.
    /// 슬러그는 프로시저가 팀명 로마자로 파생한다(UfnRomanizeKoreanSlug — 선수·마이그레이션과 단일 진실).</summary>
    public class CreateTeamInput
    {
        public Guid ManagerUserId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }
        public string? Region { get; set; }
        public List<RosterEntryDto> Roster { get; set; } = new();
    }
}
