namespace PlayGround.Client.Models
{
    /// <summary>경기 결과 (승·무·패).</summary>
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
                SoccerMatchOutcome.Win => "승",
                SoccerMatchOutcome.Draw => "무",
                _ => "패",
            };
        }
    }
}
