namespace PlayGround.Client.Services
{
    /// <summary>
    /// 온보딩 플로우 목(mock) 상태. 라우트 간 데이터 전달용 (WASM Scoped = 앱 세션 유지).
    /// 1차는 백엔드 목킹 — 실제 저장/인증은 2·3차에서 연동.
    /// </summary>
    public class OnboardingState
    {
        public string DoneFrom { get; set; } = "player";   // player | team | general
        public List<RosterEntry> Roster { get; set; } = new();
        public int RosterCount => Roster.Count;

        public void Reset()
        {
            DoneFrom = "player";
            Roster = new();
        }
    }

    public class RosterEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = "-";
        public string Number { get; set; } = "-";
    }
}
