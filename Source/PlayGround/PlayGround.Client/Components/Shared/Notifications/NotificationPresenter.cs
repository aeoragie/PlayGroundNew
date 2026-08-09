using PlayGround.Client.Localization;
using PlayGround.Client.Models;
using PlayGround.Client.Services;
using PlayGround.Contracts.Notification;
using PlayGround.Contracts.Soccer;
using PlayGround.Shared.Time;

namespace PlayGround.Client.Components.Shared.Notifications
{
    /// <summary>알림 표시 문구·딥링크·그룹·상대 시각 — 벨 패널과 알림 센터 페이지가 공유한다(문구가 어긋나면 안 된다).
    /// 딥링크는 여기서만 조립한다(SQL에 라우트를 저장하지 않는다 — Routes.cs 단일 관리).</summary>
    public static class NotificationPresenter
    {
        /// <summary>에이전트 축 알림 — feature flag(AgentApproval) OFF면 숨긴다(열람 요청·만료 임박).</summary>
        public static bool IsAgentType(NotificationDto item) =>
            item.Type == SoccerNotificationType.ViewRequest
            || item.Type == SoccerNotificationType.AgentGrantExpiring;

        public static bool IsActionType(NotificationDto item) =>
            item.Type == SoccerNotificationType.ClaimRequest
            || item.Type == SoccerNotificationType.RosterInvite;

        /// <summary>미해소 액션형 — 처리 필요(승인/거절·초대 확인 버튼이 남아 있는 상태).</summary>
        public static bool IsActionRequired(NotificationDto item)
        {
            if (item.Type == SoccerNotificationType.ClaimRequest)
            {
                return item.RequestStatus == SoccerClaimRequestStatus.Pending;
            }

            if (item.Type == SoccerNotificationType.RosterInvite)
            {
                return item.RequestStatus != SoccerClaimRequestStatus.Confirmed;
            }

            return false;
        }

        public static string GroupOf(NotificationDto item) => item.Type switch
        {
            SoccerNotificationType.ClaimRequest or
            SoccerNotificationType.ClaimApproved or
            SoccerNotificationType.ClaimRejected => AppText.Notification.GroupClaim,
            SoccerNotificationType.MatchResult => AppText.Notification.GroupMatch,
            SoccerNotificationType.ViewRequest or
            SoccerNotificationType.AgentGrantExpiring => AppText.Notification.GroupViewRequest,
            SoccerNotificationType.RosterInvite => AppText.Notification.GroupRosterInvite,
            SoccerNotificationType.TeamNotice => AppText.Notification.GroupTeamNews,
            SoccerNotificationType.ExportReady => AppText.Notification.GroupAccount,
            _ => AppText.Notification.GroupRecords,
        };

        // 딥링크 — 이동형(내비게이션형) 알림의 착지점. 없으면 null.
        public static string? RouteOf(NotificationDto item) => item.Type switch
        {
            SoccerNotificationType.ClaimApproved when item.TargetPlayerId is not null =>
                $"{Routes.PlayerDashboard}?playerId={item.TargetPlayerId}",
            SoccerNotificationType.ClaimRejected => Routes.Claim,
            SoccerNotificationType.MatchResult when item.TargetPlayerId is not null =>
                $"{Routes.PlayerDashboardSection(SoccerPlayerDashboardSection.Stats)}?playerId={item.TargetPlayerId}",
            SoccerNotificationType.CorrectionReviewed =>
                Routes.TeamDashboardSection(SoccerTeamDashboardSection.Results),
            SoccerNotificationType.ViewRequest or
            SoccerNotificationType.AgentGrantExpiring => Routes.AgentApproval(item.RefId),
            SoccerNotificationType.TeamNotice when item.TargetPlayerId is not null =>
                Routes.TeamNews(item.TargetPlayerId.Value),
            SoccerNotificationType.ExportReady => Routes.SettingsSection(SettingsSection.Account),
            _ => null,
        };

        public static string MoveTitle(NotificationDto item) => item.Type switch
        {
            SoccerNotificationType.ClaimApproved => AppText.Notification.TitleClaimApproved,
            SoccerNotificationType.ClaimRejected => AppText.Notification.TitleClaimRejected,
            SoccerNotificationType.MatchResult => AppText.Notification.TitleMatchResult,
            SoccerNotificationType.ViewRequest => AppText.Notification.TitleViewRequest,
            SoccerNotificationType.AgentGrantExpiring => AppText.Notification.TitleGrantExpiring,
            SoccerNotificationType.TeamNotice => AppText.Notification.TitleTeamNotice,
            SoccerNotificationType.ExportReady => AppText.Notification.TitleExportReady,
            _ => item.SubText == "Accepted" ? AppText.Notification.TitleCorrectionAccepted : AppText.Notification.TitleCorrectionRejected,
        };

        public static string MoveBody(NotificationDto item) => item.Type switch
        {
            SoccerNotificationType.ClaimApproved => AppText.Notification.BodyClaimApproved(item.PlayerName, item.TeamName),
            SoccerNotificationType.ClaimRejected => AppText.Notification.BodyClaimRejected(item.PlayerName, item.TeamName),
            SoccerNotificationType.MatchResult => AppText.Notification.BodyMatchResult(item.TeamName, item.ActorName, item.MetaText),
            SoccerNotificationType.ViewRequest => AppText.Notification.BodyViewRequest(item.ActorName, item.PlayerName),
            SoccerNotificationType.AgentGrantExpiring => AppText.Notification.BodyGrantExpiring(item.ActorName, item.PlayerName),
            SoccerNotificationType.TeamNotice => AppText.Notification.BodyTeamNotice(item.TeamName, item.MetaText),
            SoccerNotificationType.ExportReady => AppText.Notification.BodyExportReady,
            _ => AppText.Notification.BodyCorrection(FieldTypeLabel(item.MetaText), item.TeamName),
        };

        public static string RelationLabel(SoccerClaimRelation relation) => relation switch
        {
            SoccerClaimRelation.Father => AppText.Notification.RelationFather,
            SoccerClaimRelation.Guardian => AppText.Notification.RelationGuardian,
            _ => AppText.Notification.RelationMother,
        };

        public static string FieldTypeLabel(string? fieldType) =>
            (Enum.TryParse(fieldType, out SoccerCorrectionField field) ? field : SoccerCorrectionField.Unknown).ToLabel();

        public static string TimeAgo(SystemTime createdAtUtc)
        {
            TimeSpan span = SystemTime.Now - createdAtUtc;
            if (span.TotalMinutes < 1)
            {
                return AppText.Notification.TimeJustNow;
            }

            if (span.TotalMinutes < 60)
            {
                return AppText.Notification.TimeMinutesAgo((int)span.TotalMinutes);
            }

            if (span.TotalHours < 24)
            {
                return AppText.Notification.TimeHoursAgo((int)span.TotalHours);
            }

            if (span.TotalHours < 48)
            {
                return AppText.Notification.TimeYesterday;
            }

            DateTime local = createdAtUtc.ToWallClock();
            return AppText.Notification.TimeDate(local.Month, local.Day);
        }
    }
}
