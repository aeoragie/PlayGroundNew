namespace PlayGround.Client.Services
{
    /// <summary>온보딩 플로우 상태. 라우트 간 데이터 전달용 (WASM Scoped = 앱 세션 유지).</summary>
    public class OnboardingState
    {
        public string DoneFrom { get; set; } = "player";   // player | team | general
        public List<RosterEntry> Roster { get; set; } = new();
        public int RosterCount => Roster.Count;
        public string TeamSlug { get; set; } = string.Empty;   // 팀 생성 완료 시 서버가 준 최종 슬러그

        public void Reset()
        {
            DoneFrom = "player";
            Roster = new();
            TeamSlug = string.Empty;
        }
    }

    public class RosterEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = "-";
        public string Number { get; set; } = "-";
    }
}
