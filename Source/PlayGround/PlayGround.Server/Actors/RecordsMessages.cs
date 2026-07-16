namespace PlayGround.Server.Actors
{
    /// <summary>시즌 대회/리그 목록 조회 메시지 (읽기 — RoundRobin, 공개 화면).</summary>
    public sealed record GetSoccerRecordsTournamentsMessage(int SeasonYear);
}
