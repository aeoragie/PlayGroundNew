using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    public enum SoccerApplicantStatus
    {
        TestConfirmed,
        Reviewing,
        Waiting,
    }

    public static class SoccerApplicantStatusExtensions
    {
        public static string ToLabel(this SoccerApplicantStatus status)
        {
            return status switch
            {
                SoccerApplicantStatus.TestConfirmed => AppText.Enums.ApplicantTestConfirmed,
                SoccerApplicantStatus.Reviewing => AppText.Enums.ApplicantReviewing,
                _ => AppText.Enums.ApplicantPending,
            };
        }
    }
}
