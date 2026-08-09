using PlayGround.Client.Localization;
using PlayGround.Domain.Soccer;

namespace PlayGround.Client.Models
{
    /// <summary>enum → 화면 표기의 전이 지점은 이 파일 하나다 (Localization.md §11).
    /// 정식 라벨(값의 이름)만 여기 둔다 — 화면 문맥 카피(히어로 긴 표기·액션 버튼·상태별 본문)는 각 화면에.
    /// TODO(로컬라이징): 통과형(ToString 반환) 라벨의 국가별 표기 결정, 라벨 키의 Enums 도메인 통합.</summary>
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

        public static string ToLabel(this SoccerCorrectionStatus status) => status switch
        {
            SoccerCorrectionStatus.Accepted => AppText.Correction.StatusAccepted,
            SoccerCorrectionStatus.Rejected => AppText.Correction.StatusRejected,
            _ => AppText.Correction.StatusPending,
        };

        public static string ToLabel(this SoccerClaimRelation relation) => relation switch
        {
            SoccerClaimRelation.Father => AppText.Claim.RelationFather,
            SoccerClaimRelation.Guardian => AppText.Claim.RelationGuardian,
            _ => AppText.Claim.RelationMother,
        };

        public static string ToLabel(this SoccerAgentViewEvent eventType) => eventType switch
        {
            SoccerAgentViewEvent.RecordView => AppText.Agent.LogRecordView,
            SoccerAgentViewEvent.ProfileView => AppText.Agent.LogProfileView,
            _ => AppText.Agent.LogApproved,
        };

        public static string ToLabel(this SoccerAwardType awardType) => awardType switch
        {
            SoccerAwardType.Champion => AppText.Records.AwardChampion,
            SoccerAwardType.RunnerUp => AppText.Records.AwardRunnerUp,
            _ => AppText.Records.AwardFairPlay,
        };

        public static string? ToLabel(this SoccerPreferredFoot foot) => foot switch
        {
            SoccerPreferredFoot.Left => AppText.Enums.FootLeft,
            SoccerPreferredFoot.Right => AppText.Enums.FootRight,
            SoccerPreferredFoot.Both => AppText.Enums.FootBoth,
            _ => null,
        };

        //.// 통과형 — 현재는 로케일 중립 표기(멤버 이름) 그대로. 국가별 표기 결정 시 리소스로 승격한다.

        // TODO(로컬라이징): 포지션 표기(GK/DF/MF/FW)의 국가별 결정
        public static string? ToLabel(this SoccerPosition position) => position.ToText();

        // TODO(로컬라이징): 학년 U표기(U7~U18)의 국가별 결정 — CLAUDE.md "학년은 나이 기준 U표기"
        public static string? ToLabel(this SoccerGrade grade) => grade.ToText();

        // TODO(로컬라이징): 연령 그룹 U표기(U12/U15/U18)의 국가별 결정
        public static string? ToLabel(this SoccerAgeGroup ageGroup) => ageGroup.ToText();
    }
}
