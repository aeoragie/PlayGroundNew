using System.Text.Json.Serialization;
using PlayGround.Shared.Http;

namespace PlayGround.Domain.Account
{
    [JsonConverter(typeof(LenientEnumJsonConverter<AccountRole>))]
    public enum AccountRole
    {
        Unknown = 0,
        General,
        Player,
        Guardian,
        TeamAdmin,
        Agent,
        AgencyAdmin,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<AccountAuthProvider>))]
    public enum AccountAuthProvider
    {
        Unknown = 0,
        Local,
        Google,
        Kakao,
        Line,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<NotificationPreferenceItem>))]
    public enum NotificationPreferenceItem
    {
        Unknown = 0,
        PushChannel,
        EmailChannel,
        MatchResult,
        Recruit,
        Review,
        VisitSummary,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<DataExportStatus>))]
    public enum DataExportStatus
    {
        Unknown = 0,
        Pending,
        Ready,
        Failed,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<DataExportRequestStatus>))]
    public enum DataExportRequestStatus
    {
        Unknown = 0,
        Ok,
        InProgress,
        Cooldown,
    }
}
