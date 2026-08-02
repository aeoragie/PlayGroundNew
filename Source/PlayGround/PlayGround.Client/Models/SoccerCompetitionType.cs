using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    /// <summary>축구 대회 유형 (경기 결과 필터·pill).</summary>
    public enum SoccerCompetitionType
    {
        League,
        Cup,
        Friendly,
    }

    public static class SoccerCompetitionTypeExtensions
    {
        public static string ToLabel(this SoccerCompetitionType type)
        {
            return type switch
            {
                SoccerCompetitionType.League => AppText.Enums.CompetitionLeague,
                SoccerCompetitionType.Cup => AppText.Enums.CompetitionCup,
                _ => AppText.Enums.CompetitionFriendly,
            };
        }
    }
}
