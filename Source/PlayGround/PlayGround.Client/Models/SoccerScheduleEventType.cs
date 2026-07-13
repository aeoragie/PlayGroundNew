namespace PlayGround.Client.Models
{
    /// <summary>일정 이벤트 유형 (경기·대회·훈련).</summary>
    public enum SoccerScheduleEventType
    {
        Match,
        Tournament,
        Training,
    }

    public static class SoccerScheduleEventTypeExtensions
    {
        public static string ToLabel(this SoccerScheduleEventType type)
        {
            return type switch
            {
                SoccerScheduleEventType.Match => "경기",
                SoccerScheduleEventType.Tournament => "대회",
                _ => "훈련",
            };
        }
    }
}
