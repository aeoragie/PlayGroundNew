using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    public enum SoccerMatchOutcome
    {
        Win,
        Draw,
        Loss,
    }

    public static class SoccerMatchOutcomeExtensions
    {
        public static string ToLabel(this SoccerMatchOutcome outcome)
        {
            return outcome switch
            {
                SoccerMatchOutcome.Win => AppText.Enums.OutcomeWin,
                SoccerMatchOutcome.Draw => AppText.Enums.OutcomeDraw,
                _ => AppText.Enums.OutcomeLose,
            };
        }
    }
}
