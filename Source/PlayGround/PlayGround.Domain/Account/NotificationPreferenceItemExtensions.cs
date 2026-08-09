namespace PlayGround.Domain.Account
{
    public static class NotificationPreferenceItemExtensions
    {
        /// <summary>기본값 — 경기·모집·리뷰·푸시 켬, 방문 요약·이메일 끔 (Design.Settings).</summary>
        public static bool DefaultIsEnabled(this NotificationPreferenceItem item)
        {
            return item is NotificationPreferenceItem.PushChannel
                or NotificationPreferenceItem.MatchResult
                or NotificationPreferenceItem.Recruit
                or NotificationPreferenceItem.Review;
        }
    }
}
