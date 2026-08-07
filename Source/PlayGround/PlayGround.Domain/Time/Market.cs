namespace PlayGround.Domain.Time
{
    /// <summary>
    /// 서비스가 영업하는 시장. **업무 달력의 기준**이다 — 마감일이 며칠에 닫히는지,
    /// "올해 시즌"이 언제부터인지가 여기서 갈린다.
    ///
    /// **보는 사람의 시간대가 아니다.** 한국 팀이 올린 "8/10 마감"은 도쿄에서 봐도 한국 8/10이다.
    /// 표시 시간대는 `PlayGround.Client`의 `DisplayTime`이 따로 맡는다.
    ///
    /// 그래서 이 값은 **데이터에 붙는다** — 그 팀·대회가 어느 시장 소속인가.
    /// 계정 설정이 아니다.
    /// </summary>
    public enum Market
    {
        /// <summary>대한민국 (KST, UTC+9).</summary>
        Korea,

        /// <summary>일본 (JST, UTC+9).</summary>
        Japan,
    }
}
