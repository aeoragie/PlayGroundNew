using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    public enum SoccerVideoType
    {
        Highlight,
        FullMatch,
        Training,
    }

    public static class SoccerVideoTypeExtensions
    {
        public static string ToLabel(this SoccerVideoType type)
        {
            return type switch
            {
                SoccerVideoType.Highlight => AppText.Enums.VideoHighlight,
                SoccerVideoType.FullMatch => AppText.Enums.VideoFullMatch,
                _ => AppText.Enums.VideoTraining,
            };
        }
    }
}
