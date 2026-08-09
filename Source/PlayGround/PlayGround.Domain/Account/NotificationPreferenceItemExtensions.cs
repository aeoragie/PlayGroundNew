namespace PlayGround.Domain.Account
{
    public static class NotificationPreferenceItemExtensions
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
