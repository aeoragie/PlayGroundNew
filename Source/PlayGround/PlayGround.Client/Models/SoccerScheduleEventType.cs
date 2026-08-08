using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
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
                SoccerScheduleEventType.Match => AppText.Enums.ScheduleMatch,
                SoccerScheduleEventType.Tournament => AppText.Enums.ScheduleTournament,
                _ => AppText.Enums.ScheduleTraining,
            };
        }
    }
}
