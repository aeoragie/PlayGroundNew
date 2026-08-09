using PlayGround.Client.Localization;
using PlayGround.Domain.Soccer;

namespace PlayGround.Client.Models
{
    /// <summary>Domain 열거형의 화면 표기 라벨. **표시는 Domain이 아니라 표현 계층의 몫**이라
    /// (Domain은 Client를 참조할 수 없어 AppText에 닿지 못한다) 여기서 리소스로 라우팅한다.
    /// Domain 쪽에는 파싱·판정 같은 규칙만 남긴다. 근거는 Docs/Architecture/Localization.md §7.</summary>
    public static class SoccerDomainEnumLabels
    {
        public static string ToLabel(this SoccerCorrectionField field) => field switch
        {
            SoccerCorrectionField.Score => AppText.Enums.CorrectionFieldScore,
            SoccerCorrectionField.GoalAssist => AppText.Enums.CorrectionFieldGoalAssist,
            SoccerCorrectionField.Appearance => AppText.Enums.CorrectionFieldAppearance,
            _ => AppText.Enums.CorrectionFieldOther,
        };

        /// <summary>DB 저장 문자열 → 항목 라벨. 알 수 없으면 '기타'.</summary>
        public static string ToCorrectionFieldLabel(string? fieldType)
        {
            SoccerCorrectionFieldExtensions.TryParse(fieldType, out SoccerCorrectionField field);
            return field.ToLabel();
        }

        public static string ToTagLabel(this SoccerCareerOutcomeType type) => type switch
        {
            SoccerCareerOutcomeType.ProTransfer => AppText.Enums.CareerOutcomeTagProTransfer,
            SoccerCareerOutcomeType.SchoolTeam => AppText.Enums.CareerOutcomeTagSchoolTeam,
            _ => AppText.Enums.CareerOutcomeTagPromotion,
        };

        public static string ToSummaryLabel(this SoccerCareerOutcomeType type) => type switch
        {
            SoccerCareerOutcomeType.ProTransfer => AppText.Enums.CareerOutcomeSummaryProTransfer,
            SoccerCareerOutcomeType.SchoolTeam => AppText.Enums.CareerOutcomeSummarySchoolTeam,
            _ => AppText.Enums.CareerOutcomeSummaryPromotion,
        };
    }
}
