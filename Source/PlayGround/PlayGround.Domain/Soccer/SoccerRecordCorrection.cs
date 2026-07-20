namespace PlayGround.Domain.Soccer
{
    /// <summary>수정 신청 대상 항목. **1건 1항목** — 여러 오류는 신청을 여러 건 올린다(심사 단위 명확화).
    /// DB 저장 문자열과 멤버 이름이 같다.</summary>
    public enum SoccerCorrectionField
    {
        /// <summary>스코어 — 숫자 2칸.</summary>
        Score,
        /// <summary>득점·도움 — 선수 선택.</summary>
        GoalAssist,
        /// <summary>출전 선수 — 선수 선택.</summary>
        Appearance,
        /// <summary>그 외 — 설명으로 전달.</summary>
        Other,
    }

    /// <summary>신청 상태. **Pending에서 다음으로 넘기는 것은 주최측(대회 운영 서비스)의 몫**이다 —
    /// PlayGround는 생성·조회·취소만 한다(설계 결정 6·7).</summary>
    public enum SoccerCorrectionStatus
    {
        /// <summary>접수 — 심사 대기. 이 상태에서만 신청자가 취소할 수 있다.</summary>
        Pending,
        /// <summary>반영 — 주최측이 기록을 고쳤다.</summary>
        Accepted,
        /// <summary>반려 — 사유가 함께 온다.</summary>
        Rejected,
    }

    public static class SoccerCorrectionFieldExtensions
    {
        public static string ToLabel(this SoccerCorrectionField field) => field switch
        {
            SoccerCorrectionField.Score => "스코어",
            SoccerCorrectionField.GoalAssist => "득점·도움",
            SoccerCorrectionField.Appearance => "출전 선수",
            _ => "기타",
        };

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

        public static string ToLabel(this SoccerCorrectionStatus status) => status switch
        {
            SoccerCorrectionStatus.Accepted => "반영",
            SoccerCorrectionStatus.Rejected => "반려",
            _ => "접수",
        };
    }
}
