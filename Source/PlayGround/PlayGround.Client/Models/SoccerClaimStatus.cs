namespace PlayGround.Client.Models
{
    /// <summary>선수 프로필 가족 연결(Claim) 상태. 화면 표기는 영문 이름 그대로 (SPEC.TEAMDASHBOARD.md).</summary>
    public enum SoccerClaimStatus
    {
        Claimed,
        Pending,
        Unclaimed,
    }
}
