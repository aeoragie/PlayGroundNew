using PlayGround.Contracts.Common;

namespace PlayGround.Client.Models
{
    /// <summary>알림 항목 기본값의 표시층 사본 — 서버 원본은 Application.Settings.NotificationPreferenceDefaults.
    /// Client는 Application을 참조할 수 없어 사본을 둔다. 정책이 바뀌면 둘을 함께 고친다 (Design.Settings).</summary>
    public static class NotificationPreferenceDefaults
    {
        public static bool DefaultIsEnabled(this NotificationPreferenceItem item)
        {
            return item is NotificationPreferenceItem.PushChannel
                or NotificationPreferenceItem.MatchResult
                or NotificationPreferenceItem.Recruit
                or NotificationPreferenceItem.Review;
        }
    }
}
