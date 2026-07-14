namespace PlayGround.Client.Models
{
    /// <summary>선수 프로필 가족 연결(Claim) 상태. 화면 표기는 영문 이름 그대로 (SPEC.TEAMDASHBOARD.md).</summary>
    public enum SoccerClaimStatus
    {
        Claimed,
        Pending,
        Unclaimed,
    }

    public static class SoccerClaimStatusExtensions
    {
        /// <summary>서버 문자열 → enum. 알 수 없는 값은 Unclaimed (안전 기본값).</summary>
        public static SoccerClaimStatus ParseClaimStatus(string? value)
        {
            return Enum.TryParse(value, out SoccerClaimStatus status) ? status : SoccerClaimStatus.Unclaimed;
        }
    }
}
