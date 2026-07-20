namespace PlayGround.Domain.Soccer
{
    /// <summary>
    /// "처리가 필요해요" 항목의 유형 (Design.DashboardHub §3). 유형별로 칩 색이 다르다.
    ///
    /// SPEC은 연결·열람·결과 3종을 정의하지만 **열람(에이전트)은 축 자체가 미구현**이라
    /// 지금 만들 수 있는 것은 2종뿐이다. 에이전트 열람 승인이 생기면 여기에 Access를 더한다.
    /// </summary>
    public enum SoccerActionKind
    {
        /// <summary>연결 — 초대코드 미처리. 오렌지 톤.</summary>
        Invite,

        /// <summary>결과 — 기록 수정 신청의 심사 결과. 네이비 톤.</summary>
        Correction,
    }

    public static class SoccerActionKindExtensions
    {
        public static SoccerActionKind Parse(string? value) =>
            Enum.TryParse(value, out SoccerActionKind parsed) ? parsed : SoccerActionKind.Invite;
    }
}
