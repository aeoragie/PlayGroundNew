namespace PlayGround.Client.Models
{
    /// <summary>대회/리그 형식. DB 저장 문자열 = 멤버 이름 (SoccerTournaments.Format).
    /// 상세 화면의 탭 구성을 결정한다.</summary>
    public enum SoccerTournamentFormat
    {
        Cup,     // 조별 예선 + 토너먼트
        Split,   // 1차 풀리그 + 2차 스플릿
        League,  // 단일 리그
    }

    public static class SoccerTournamentFormatExtensions
    {
        /// <summary>목록 행 형식 뱃지 라벨.</summary>
        public static string ToLabel(this SoccerTournamentFormat format)
        {
            return format switch
            {
                SoccerTournamentFormat.Cup => "조별+토너먼트",
                SoccerTournamentFormat.Split => "풀리그+스플릿",
                _ => "리그",
            };
        }
    }
}
