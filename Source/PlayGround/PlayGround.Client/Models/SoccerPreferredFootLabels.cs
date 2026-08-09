using PlayGround.Client.Localization;
using PlayGround.Contracts.Soccer;

namespace PlayGround.Client.Models
{
    public static class SoccerPreferredFootLabels
    {
        public static string? ToLabel(this SoccerPreferredFoot foot)
        {
            return foot switch
            {
                SoccerPreferredFoot.Left => AppText.Enums.FootLeft,
                SoccerPreferredFoot.Right => AppText.Enums.FootRight,
                SoccerPreferredFoot.Both => AppText.Enums.FootBoth,
                _ => null,
            };
        }
    }
}
