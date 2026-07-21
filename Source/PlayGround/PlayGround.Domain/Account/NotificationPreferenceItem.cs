namespace PlayGround.Domain.Account
{
    /// <summary>
    /// 알림 설정 항목 (채널 + 알림 유형). 멤버 이름 = DB 저장 문자열 (NotificationPreferences.ItemName).
    /// **승인형(연결 요청·열람 요청)은 여기에 없다** — 미성년자 보호 관문이라 항상 켜짐이며,
    /// 이 enum이 저장 화이트리스트이므로 승인형은 클라이언트가 우회해도 저장이 거부된다.
    /// </summary>
    public enum NotificationPreferenceItem
    {
        PushChannel,
        EmailChannel,
        MatchResult,
        Recruit,
        Review,
        VisitSummary,
    }

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
