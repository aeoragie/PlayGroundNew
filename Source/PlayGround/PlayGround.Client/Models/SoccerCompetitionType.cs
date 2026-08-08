using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
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
