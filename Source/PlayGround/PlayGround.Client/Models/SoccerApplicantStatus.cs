using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    /// <summary>선수 모집 지원자 상태.</summary>
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
