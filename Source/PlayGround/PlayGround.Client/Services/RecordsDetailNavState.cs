namespace PlayGround.Client.Services
{
    /// <summary>Records 대회 상세 → 경기 상세 왕복 시 복귀 상태 보관 (직전 탭·조·필터·확장 행·스크롤).
    /// WASM에서 Scoped = 앱 생명주기 싱글턴. 경기 상세로 이동 전 Save, 대회 상세 복귀 시 TryConsume.</summary>
    public class RecordsDetailNavState
    {
        public Guid TournamentId { get; private set; }
        public string Tab { get; private set; } = string.Empty;
        public string Group { get; private set; } = string.Empty;
        public string Round { get; private set; } = string.Empty;
        public string KnockoutRound { get; private set; } = string.Empty;
        public string Month { get; private set; } = string.Empty;
        public Guid OpenMatchId { get; private set; }
        public double ScrollY { get; private set; }

        private bool mHasPending;

        public void Save(Guid tournamentId, string tab, string group, string round, string knockoutRound, string month, Guid openMatchId, double scrollY)
        {
            TournamentId = tournamentId;
            Tab = tab;
            Group = group;
            Round = round;
            KnockoutRound = knockoutRound;
            Month = month;
            OpenMatchId = openMatchId;
            ScrollY = scrollY;
            mHasPending = true;
        }

        /// <summary>대회 상세 복귀 시 소비 — 같은 대회의 보류 상태가 있으면 true(필드 반영 대상), 아니면 false.</summary>
        public bool TryConsume(Guid tournamentId)
        {
            if (!mHasPending || TournamentId != tournamentId)
            {
                return false;
            }

            mHasPending = false;
            return true;
        }
    }
}
