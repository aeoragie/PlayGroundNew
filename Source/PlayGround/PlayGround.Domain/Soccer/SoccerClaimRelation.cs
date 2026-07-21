namespace PlayGround.Domain.Soccer
{
    /// <summary>Claim 플로우의 보호자 관계. 멤버 이름 = DB 저장 문자열
    /// (SoccerPlayerClaimRequests.Relation · SoccerPlayerFamilyLinks.Relation). 표시 라벨은 클라이언트.</summary>
    public enum SoccerClaimRelation
    {
        Mother,
        Father,
        Guardian,
    }
}
