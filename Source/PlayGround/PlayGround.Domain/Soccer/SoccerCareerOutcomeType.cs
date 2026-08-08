namespace PlayGround.Domain.Soccer
{
    /// <summary>진학·진로 사례 유형 (Design.TeamPublicHome ⑤). 멤버 이름 = DB 저장 문자열 (SoccerTeamCareerOutcomes.OutcomeType).</summary>
    public enum SoccerCareerOutcomeType
    {
        ProTransfer,

        SchoolTeam,

        Promotion,
    }

    // 표시 라벨(태그·요약)은 Domain이 아니라 표현 계층이 가진다 — Client의 SoccerDomainEnumLabels 참조.
}
