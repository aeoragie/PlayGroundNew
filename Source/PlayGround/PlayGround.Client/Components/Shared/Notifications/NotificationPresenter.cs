using System;
using PlayGround.Contracts.Notification;
using PlayGround.Domain.Soccer;
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
            nameof(SoccerNotificationType.ClaimRejected) => "연결 요청",
            nameof(SoccerNotificationType.MatchResult) => "경기",
            nameof(SoccerNotificationType.ViewRequest) or
            nameof(SoccerNotificationType.AgentGrantExpiring) => "열람 요청",
            nameof(SoccerNotificationType.RosterInvite) => "선수단 초대",
            nameof(SoccerNotificationType.TeamNotice) => "팀 소식",
            nameof(SoccerNotificationType.ExportReady) => "내 계정",
            _ => "기록",
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
            nameof(SoccerNotificationType.ClaimApproved) => "프로필 연결 승인",
            nameof(SoccerNotificationType.ClaimRejected) => "연결 요청이 거절됐어요",
            nameof(SoccerNotificationType.MatchResult) => "경기 결과 반영",
            nameof(SoccerNotificationType.ViewRequest) => "상세 정보 열람 요청",
            nameof(SoccerNotificationType.AgentGrantExpiring) => "열람 승인 만료 임박",
            nameof(SoccerNotificationType.TeamNotice) => "팀 공지",
            nameof(SoccerNotificationType.ExportReady) => "데이터 파일이 준비됐어요",
            _ => item.SubText == "Accepted" ? "기록 수정 신청 반영" : "기록 수정 신청 반려",
        };

        public static string MoveBody(NotificationDto item) => item.Type switch
        {
            nameof(SoccerNotificationType.ClaimApproved) => $"{item.PlayerName} 선수와 연결됐어요 — {item.TeamName}",
            nameof(SoccerNotificationType.ClaimRejected) => $"{item.PlayerName} · {item.TeamName} — 팀에 문의해 보세요",
            nameof(SoccerNotificationType.MatchResult) => $"{item.TeamName} vs {item.ActorName} ({item.MetaText}) 결과가 등록됐어요",
            nameof(SoccerNotificationType.ViewRequest) => $"{item.ActorName} 님이 {item.PlayerName} 선수의 상세 정보 열람을 요청했어요",
            nameof(SoccerNotificationType.AgentGrantExpiring) => $"{item.ActorName} 님의 {item.PlayerName} 선수 열람 권한이 3일 안에 만료돼요",
            nameof(SoccerNotificationType.TeamNotice) => $"{item.TeamName} · {item.MetaText}",
            nameof(SoccerNotificationType.ExportReady) => "설정 · 계정에서 7일 안에 내려받을 수 있어요",
            _ => $"{FieldTypeLabel(item.MetaText)} 항목 · {item.TeamName}",
        };

        public static string RelationLabel(string? relation) => relation switch
        {
            "Father" => "아버지",
            "Guardian" => "기타 보호자",
            _ => "어머니",
        };

        // 기록 수정 신청 항목 라벨 (B6 폼 4항목과 동일)
        public static string FieldTypeLabel(string? fieldType) => fieldType switch
        {
            "Score" => "스코어",
            "GoalAssist" => "득점·도움",
            "Appearance" => "출전 선수",
            _ => "기타",
        };

        public static string TimeAgo(DateTime createdAtUtc)
        {
            TimeSpan span = DateTime.UtcNow - createdAtUtc;
            if (span.TotalMinutes < 1)
            {
                return "방금 전";
            }

            if (span.TotalMinutes < 60)
            {
                return $"{(int)span.TotalMinutes}분 전";
            }

            if (span.TotalHours < 24)
            {
                return $"{(int)span.TotalHours}시간 전";
            }

            if (span.TotalHours < 48)
            {
                return "어제";
            }

            DateTime local = createdAtUtc.ToLocalTime();
            return $"{local.Month}/{local.Day}";
        }
    }
}
