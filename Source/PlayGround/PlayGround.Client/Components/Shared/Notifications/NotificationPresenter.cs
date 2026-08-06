using PlayGround.Shared.Time;
using System;
using PlayGround.Contracts.Notification;
using PlayGround.Domain.Soccer;
using PlayGround.Client.Localization;
using PlayGround.Client.Models;

namespace PlayGround.Client.Components.Shared.Notifications
{
    /// <summary>알림 표시 문구·딥링크·그룹·상대 시각 — 벨 패널과 알림 센터 페이지가 공유한다(문구가 어긋나면 안 된다).
    /// 딥링크는 여기서만 조립한다(SQL에 라우트를 저장하지 않는다 — Routes.cs 단일 관리).</summary>
    public static class NotificationPresenter
    {
        /// <summary>에이전트 축 알림 — feature flag(AgentApproval) OFF면 숨긴다(열람 요청·만료 임박).</summary>
        public static bool IsAgentType(NotificationDto item) =>
            item.Type == nameof(SoccerNotificationType.ViewRequest)
            || item.Type == nameof(SoccerNotificationType.AgentGrantExpiring);

        /// <summary>액션형(인라인 처리 대상) — 처리 필요 세그먼트·행 우측 버튼 판정.</summary>
        public static bool IsActionType(NotificationDto item) =>
            item.Type == nameof(SoccerNotificationType.ClaimRequest)
            || item.Type == nameof(SoccerNotificationType.RosterInvite);

        /// <summary>미해소 액션형 — 처리 필요(승인/거절·초대 확인 버튼이 남아 있는 상태).</summary>
        public static bool IsActionRequired(NotificationDto item)
        {
            if (item.Type == nameof(SoccerNotificationType.ClaimRequest))
            {
                return item.RequestStatus == "Pending";
            }

            if (item.Type == nameof(SoccerNotificationType.RosterInvite))
            {
                return item.RequestStatus != "Confirmed";
            }

            return false;
        }

        // 유형 → 칩 그룹 (존재하는 것만 노출)
        public static string GroupOf(NotificationDto item) => item.Type switch
        {
            nameof(SoccerNotificationType.ClaimRequest) or
            nameof(SoccerNotificationType.ClaimApproved) or
            nameof(SoccerNotificationType.ClaimRejected) => AppText.Notification.GroupClaim,
            nameof(SoccerNotificationType.MatchResult) => AppText.Notification.GroupMatch,
            nameof(SoccerNotificationType.ViewRequest) or
            nameof(SoccerNotificationType.AgentGrantExpiring) => AppText.Notification.GroupViewRequest,
            nameof(SoccerNotificationType.RosterInvite) => AppText.Notification.GroupRosterInvite,
            nameof(SoccerNotificationType.TeamNotice) => AppText.Notification.GroupTeamNews,
            nameof(SoccerNotificationType.ExportReady) => AppText.Notification.GroupAccount,
            _ => AppText.Notification.GroupRecords,
        };

        // 딥링크 — 이동형(내비게이션형) 알림의 착지점. 없으면 null.
        public static string? RouteOf(NotificationDto item) => item.Type switch
        {
            nameof(SoccerNotificationType.ClaimApproved) when item.TargetPlayerId is not null =>
                $"{Routes.PlayerDashboard}?playerId={item.TargetPlayerId}",
            nameof(SoccerNotificationType.ClaimRejected) => Routes.Claim,
            nameof(SoccerNotificationType.MatchResult) when item.TargetPlayerId is not null =>
                $"{Routes.PlayerDashboardSection(SoccerPlayerDashboardSection.Stats)}?playerId={item.TargetPlayerId}",
            nameof(SoccerNotificationType.CorrectionReviewed) =>
                Routes.TeamDashboardSection(SoccerTeamDashboardSection.Results),
            nameof(SoccerNotificationType.ViewRequest) or
            nameof(SoccerNotificationType.AgentGrantExpiring) => Routes.AgentApproval(item.RefId),
            nameof(SoccerNotificationType.TeamNotice) when item.TargetPlayerId is not null =>
                Routes.TeamNews(item.TargetPlayerId.Value),
            nameof(SoccerNotificationType.ExportReady) => Routes.SettingsSection(SettingsSection.Account),
            _ => null,
        };

        public static string MoveTitle(NotificationDto item) => item.Type switch
        {
            nameof(SoccerNotificationType.ClaimApproved) => AppText.Notification.TitleClaimApproved,
            nameof(SoccerNotificationType.ClaimRejected) => AppText.Notification.TitleClaimRejected,
            nameof(SoccerNotificationType.MatchResult) => AppText.Notification.TitleMatchResult,
            nameof(SoccerNotificationType.ViewRequest) => AppText.Notification.TitleViewRequest,
            nameof(SoccerNotificationType.AgentGrantExpiring) => AppText.Notification.TitleGrantExpiring,
            nameof(SoccerNotificationType.TeamNotice) => AppText.Notification.TitleTeamNotice,
            nameof(SoccerNotificationType.ExportReady) => AppText.Notification.TitleExportReady,
            _ => item.SubText == "Accepted" ? AppText.Notification.TitleCorrectionAccepted : AppText.Notification.TitleCorrectionRejected,
        };

        public static string MoveBody(NotificationDto item) => item.Type switch
        {
            nameof(SoccerNotificationType.ClaimApproved) => AppText.Notification.BodyClaimApproved(item.PlayerName, item.TeamName),
            nameof(SoccerNotificationType.ClaimRejected) => AppText.Notification.BodyClaimRejected(item.PlayerName, item.TeamName),
            nameof(SoccerNotificationType.MatchResult) => AppText.Notification.BodyMatchResult(item.TeamName, item.ActorName, item.MetaText),
            nameof(SoccerNotificationType.ViewRequest) => AppText.Notification.BodyViewRequest(item.ActorName, item.PlayerName),
            nameof(SoccerNotificationType.AgentGrantExpiring) => AppText.Notification.BodyGrantExpiring(item.ActorName, item.PlayerName),
            nameof(SoccerNotificationType.TeamNotice) => AppText.Notification.BodyTeamNotice(item.TeamName, item.MetaText),
            nameof(SoccerNotificationType.ExportReady) => AppText.Notification.BodyExportReady,
            _ => AppText.Notification.BodyCorrection(FieldTypeLabel(item.MetaText), item.TeamName),
        };

        public static string RelationLabel(string? relation) => relation switch
        {
            "Father" => AppText.Notification.RelationFather,
            "Guardian" => AppText.Notification.RelationGuardian,
            _ => AppText.Notification.RelationMother,
        };

        // 기록 수정 신청 항목 라벨 — 폼(B6)과 같은 리소스를 쓴다
        public static string FieldTypeLabel(string? fieldType) =>
            SoccerDomainEnumLabels.ToCorrectionFieldLabel(fieldType);

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

            DateTime local = createdAtUtc.ToLocalTime();
            return AppText.Notification.TimeDate(local.Month, local.Day);
        }
    }
}
