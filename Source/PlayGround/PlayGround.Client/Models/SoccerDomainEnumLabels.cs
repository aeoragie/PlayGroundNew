using PlayGround.Client.Localization;
using PlayGround.Domain.Soccer;

namespace PlayGround.Client.Models
{
    /// <summary>Contracts 열거형의 화면 표기 라벨. **표시는 표현 계층의 몫**이라
    /// (Contracts는 Client를 참조할 수 없어 AppText에 닿지 못한다) 여기서 리소스로 라우팅한다.
    /// 근거는 Docs/Architecture/Localization.md §7.</summary>
    public static class SoccerDomainEnumLabels
    {
        public static string ToLabel(this SoccerCorrectionField field) => field switch
        {
            SoccerCorrectionField.Score => AppText.Enums.CorrectionFieldScore,
            SoccerCorrectionField.GoalAssist => AppText.Enums.CorrectionFieldGoalAssist,
            SoccerCorrectionField.Appearance => AppText.Enums.CorrectionFieldAppearance,
            _ => AppText.Enums.CorrectionFieldOther,
        };

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

        public static string ToLabel(this SoccerCompetitionType type) => type switch
        {
            SoccerCompetitionType.League => AppText.Enums.CompetitionLeague,
            SoccerCompetitionType.Cup => AppText.Enums.CompetitionCup,
            _ => AppText.Enums.CompetitionFriendly,
        };

        public static string ToLabel(this SoccerTournamentFormat format) => format switch
        {
            SoccerTournamentFormat.Cup => AppText.Enums.TournamentFormatCup,
            SoccerTournamentFormat.Split => AppText.Enums.TournamentFormatSplit,
            _ => AppText.Enums.TournamentFormatLeague,
        };

        public static string ToLabel(this SoccerTournamentStatus status) => status switch
        {
            SoccerTournamentStatus.InProgress => AppText.Enums.TournamentInProgress,
            SoccerTournamentStatus.Scheduled => AppText.Enums.TournamentScheduled,
            _ => AppText.Enums.TournamentEnded,
        };

        /// <summary>대회 목록 자동 정렬 순서 (진행중 0 → 예정 1 → 종료 2).</summary>
        public static int SortOrder(this SoccerTournamentStatus status)
        {
            return status switch
            {
                SoccerTournamentStatus.InProgress => 0,
                SoccerTournamentStatus.Scheduled => 1,
                _ => 2,
            };
        }

        public static string ToLabel(this SoccerScheduleType type) => type switch
        {
            SoccerScheduleType.Match => AppText.Enums.ScheduleMatch,
            SoccerScheduleType.Tournament => AppText.Enums.ScheduleTournament,
            _ => AppText.Enums.ScheduleTraining,
        };

        public static string ToLabel(this SoccerVideoType type) => type switch
        {
            SoccerVideoType.Highlight => AppText.Enums.VideoHighlight,
            SoccerVideoType.FullMatch => AppText.Enums.VideoFullMatch,
            _ => AppText.Enums.VideoTraining,
        };
    }
}
