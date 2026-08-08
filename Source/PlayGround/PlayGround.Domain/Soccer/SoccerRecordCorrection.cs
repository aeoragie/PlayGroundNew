namespace PlayGround.Domain.Soccer
{
    /// <summary>수정 신청 대상 항목. **1건 1항목** — 여러 오류는 신청을 여러 건 올린다(심사 단위 명확화).
    /// DB 저장 문자열과 멤버 이름이 같다.</summary>
    public enum SoccerCorrectionField
    {
        Score,
        GoalAssist,
        Appearance,
        Other,
    }

    /// <summary>신청 상태. **Pending에서 다음으로 넘기는 것은 주최측(대회 운영 서비스)의 몫**이다 —
    /// PlayGround는 생성·조회·취소만 한다(설계 결정 6·7).</summary>
    public enum SoccerCorrectionStatus
    {
        Pending,
        Accepted,
        Rejected,
    }

    public static class SoccerCorrectionFieldExtensions
    {
        // 표시 라벨은 Domain이 아니라 표현 계층이 가진다 — Client의 SoccerDomainEnumLabels 참조.

        public static bool TryParse(string? value, out SoccerCorrectionField field)
        {
            // 숫자 문자열이 enum으로 파싱되는 것을 막는다 (멤버 이름만 허용)
            if (!string.IsNullOrWhiteSpace(value) && !char.IsAsciiDigit(value[0])
                && Enum.TryParse(value, out field))
            {
                return true;
            }

            field = SoccerCorrectionField.Other;
            return false;
        }
    }

    public static class SoccerCorrectionStatusExtensions
    {
        public static SoccerCorrectionStatus Parse(string? value) =>
            Enum.TryParse(value, out SoccerCorrectionStatus parsed) ? parsed : SoccerCorrectionStatus.Pending;
    }
}
