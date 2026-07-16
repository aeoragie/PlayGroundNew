namespace PlayGround.Client.Models
{
    /// <summary>대회 진행 상태. DB 저장 문자열 = 멤버 이름 (SoccerTournaments.Status).
    /// 목록 정렬 = 진행중 → 예정 → 종료 (선언 순서가 아니라 SortOrder 사용).</summary>
    public enum SoccerTournamentStatus
    {
        Scheduled,   // 예정
        InProgress,  // 진행중
        Completed,   // 종료
    }

    public static class SoccerTournamentStatusExtensions
    {
        public static string ToLabel(this SoccerTournamentStatus status)
        {
            return status switch
            {
                SoccerTournamentStatus.InProgress => "진행중",
                SoccerTournamentStatus.Scheduled => "예정",
                _ => "종료",
            };
        }

        /// <summary>목록 자동 정렬 순서 (진행중 0 → 예정 1 → 종료 2).</summary>
        public static int SortOrder(this SoccerTournamentStatus status)
        {
            return status switch
            {
                SoccerTournamentStatus.InProgress => 0,
                SoccerTournamentStatus.Scheduled => 1,
                _ => 2,
            };
        }
    }
}
