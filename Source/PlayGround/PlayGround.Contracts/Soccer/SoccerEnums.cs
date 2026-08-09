using System.Text.Json.Serialization;
using PlayGround.Shared.Http;

namespace PlayGround.Contracts.Soccer
{
    // 와이어(JSON)는 멤버 이름 문자열, DB도 멤버 이름 문자열(변환은 Persistence EnumColumn에서만).
    // Unknown(0)은 저장·전송 값이 아니라 미지정·미지 값 폴백이다.

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerAgeGroup>))]
    public enum SoccerAgeGroup
    {
        Unknown = 0,
        U12,
        U15,
        U18,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerPosition>))]
    public enum SoccerPosition
    {
        Unknown = 0,
        GK,
        DF,
        MF,
        FW,
    }

    /// <summary>학년 — 국가 학제 대신 나이 기준 U표기. 표시도 당분간 이 표기 그대로다(국가별 표기는 추후 결정).</summary>
    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerGrade>))]
    public enum SoccerGrade
    {
        Unknown = 0,
        U7,
        U8,
        U9,
        U10,
        U11,
        U12,
        U13,
        U14,
        U15,
        U16,
        U17,
        U18,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerPreferredFoot>))]
    public enum SoccerPreferredFoot
    {
        Unknown = 0,
        Left,
        Right,
        Both,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerMatchType>))]
    public enum SoccerMatchType
    {
        Unknown = 0,
        Official,
        Friendly,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerCompetitionType>))]
    public enum SoccerCompetitionType
    {
        Unknown = 0,
        League,
        Cup,
        Friendly,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerMatchStatus>))]
    public enum SoccerMatchStatus
    {
        Unknown = 0,
        Scheduled,
        Completed,
        Canceled,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerTournamentStatus>))]
    public enum SoccerTournamentStatus
    {
        Unknown = 0,
        Scheduled,
        InProgress,
        Completed,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerTournamentFormat>))]
    public enum SoccerTournamentFormat
    {
        Unknown = 0,
        Cup,
        Split,
        League,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerTournamentScope>))]
    public enum SoccerTournamentScope
    {
        Unknown = 0,
        National,
        Regional,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerStageType>))]
    public enum SoccerStageType
    {
        Unknown = 0,
        Group,
        Split1,
        Split2,
        Knockout,
        League,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerMatchEventType>))]
    public enum SoccerMatchEventType
    {
        Unknown = 0,
        Goal,
        OwnGoal,
        PenaltyGoal,
        YellowCard,
        RedCard,
        Substitution,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerAwardType>))]
    public enum SoccerAwardType
    {
        Unknown = 0,
        Champion,
        RunnerUp,
        FairPlay,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerVideoType>))]
    public enum SoccerVideoType
    {
        Unknown = 0,
        Highlight,
        FullMatch,
        Training,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerScheduleType>))]
    public enum SoccerScheduleType
    {
        Unknown = 0,
        Match,
        Tournament,
        Training,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerChannelType>))]
    public enum SoccerChannelType
    {
        Unknown = 0,
        YouTube,
        Instagram,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerFamilyRole>))]
    public enum SoccerFamilyRole
    {
        Unknown = 0,
        Guardian,
        Self,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerRosterClaimStatus>))]
    public enum SoccerRosterClaimStatus
    {
        Unknown = 0,
        Unclaimed,
        Pending,
        Claimed,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerClaimRelation>))]
    public enum SoccerClaimRelation
    {
        Unknown = 0,
        Mother,
        Father,
        Guardian,
    }

    /// <summary>Confirmed는 저장 값이 아니라 알림 조회의 파생 상태다 (RosterInvite — SoccerApplications.ConfirmedAt).</summary>
    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerClaimRequestStatus>))]
    public enum SoccerClaimRequestStatus
    {
        Unknown = 0,
        Pending,
        Approved,
        Rejected,
        Confirmed,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerApplicationStatus>))]
    public enum SoccerApplicationStatus
    {
        Unknown = 0,
        Pending,
        Reviewing,
        Accepted,
        Rejected,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerApplicationRoute>))]
    public enum SoccerApplicationRoute
    {
        Unknown = 0,
        Direct,
        AgentRef,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerRecruitmentStatus>))]
    public enum SoccerRecruitmentStatus
    {
        Unknown = 0,
        Open,
        Closed,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerAgentRequestStatus>))]
    public enum SoccerAgentRequestStatus
    {
        Unknown = 0,
        Pending,
        Approved,
        Denied,
        Revoked,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerAgentEligibility>))]
    public enum SoccerAgentEligibility
    {
        Unknown = 0,
        NotAgent,
        Blocked,
        Active,
        Cooldown,
        Allowed,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerAgentViewEvent>))]
    public enum SoccerAgentViewEvent
    {
        Unknown = 0,
        Approved,
        ProfileView,
        RecordView,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerAgentReviewAction>))]
    public enum SoccerAgentReviewAction
    {
        Unknown = 0,
        Approve,
        Deny,
        Revoke,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerCareerOutcomeType>))]
    public enum SoccerCareerOutcomeType
    {
        Unknown = 0,
        ProTransfer,
        SchoolTeam,
        Promotion,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerTeamPostType>))]
    public enum SoccerTeamPostType
    {
        Unknown = 0,
        Notice,
        Material,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerNotificationType>))]
    public enum SoccerNotificationType
    {
        Unknown = 0,
        ClaimRequest,
        ClaimApproved,
        ClaimRejected,
        MatchResult,
        CorrectionReviewed,
        ViewRequest,
        RosterInvite,
        TeamNotice,
        ExportReady,
        AgentGrantExpiring,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerPlayerProfileField>))]
    public enum SoccerPlayerProfileField
    {
        Unknown = 0,
        Profile,
        Height,
        Weight,
        PreferredFoot,
        School,
        GuardianPhone,
        StrengthTags,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerCorrectionField>))]
    public enum SoccerCorrectionField
    {
        Unknown = 0,
        Score,
        GoalAssist,
        Appearance,
        Other,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerCorrectionStatus>))]
    public enum SoccerCorrectionStatus
    {
        Unknown = 0,
        Pending,
        Accepted,
        Rejected,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerActionKind>))]
    public enum SoccerActionKind
    {
        Unknown = 0,
        Invite,
        Correction,
    }
}
