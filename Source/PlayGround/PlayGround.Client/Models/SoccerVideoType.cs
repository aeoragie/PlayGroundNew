using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    /// <summary>경기영상 유형 (필터·pill).</summary>
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
