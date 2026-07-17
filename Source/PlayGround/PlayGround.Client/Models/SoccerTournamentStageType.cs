namespace PlayGround.Client.Models
{
    /// <summary>대회 스테이지 종류. DB 저장 문자열 = 멤버 이름 (SoccerMatches/Standings.StageType).</summary>
    public enum SoccerTournamentStageType
    {
        Group,     // 조별 예선
        Split1,    // 1차 풀리그
        Split2,    // 2차 스플릿리그
        Knockout,  // 토너먼트
        League,    // 리그
    }
}
