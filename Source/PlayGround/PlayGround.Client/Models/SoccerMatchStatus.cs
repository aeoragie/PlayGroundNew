namespace PlayGround.Client.Models
{
    /// <summary>경기 상태. DB 저장 문자열 = 멤버 이름 (SoccerMatches.Status).</summary>
    public enum SoccerMatchStatus
    {
        Scheduled,   // 예정
        Completed,   // 종료
        Canceled,    // 취소
    }
}
