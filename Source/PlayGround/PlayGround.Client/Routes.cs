using PlayGround.Client.Models;

namespace PlayGround.Client
{
    /// <summary>
    /// 클라이언트 페이지 라우트 단일 관리.
    /// 페이지 선언은 @attribute [Route(Routes.X)]로, 링크·NavigateTo는 상수/헬퍼로 여기만 참조한다.
    /// (API 엔드포인트 URL은 각 Services/*Client가 관리 — 여기는 페이지 라우트만.)
    /// </summary>
    public static class Routes
    {
        //.// 페이지 템플릿 (@attribute [Route(...)] — {파라미터} 자리표시자 포함)

        public const string Landing = "/";
        public const string SoccerLanding = "/soccer";
        public const string Login = "/login";
        public const string SelectRole = "/settings/select-role";
        public const string PlayerOnboarding = "/onboarding/player";
        public const string TeamOnboarding = "/onboarding/team";
        public const string OnboardingComplete = "/onboarding/complete";
        public const string Dashboard = "/dashboard";
        public const string TeamDashboard = "/dashboard/team";
        public const string TeamDashboardSectionTemplate = "/dashboard/team/{Section}";
        public const string PlayerDashboard = "/dashboard/player";
        public const string PlayerDashboardSectionTemplate = "/dashboard/player/{Section}";
        public const string TeamPublicHomeTemplate = "/team/{Slug}";
        public const string TeamPublicHomeTabTemplate = "/team/{Slug}/{Tab}";
        public const string Records = "/records";
        public const string RecordsArchive = "/records/archive";
        public const string NotFound = "/not-found";

        //.// 링크 생성 헬퍼 (파라미터 라우트)

        public static string TeamDashboardSection(SoccerTeamDashboardSection section)
        {
            return $"{TeamDashboard}/{section.ToSlug()}";
        }

        public static string PlayerDashboardSection(SoccerPlayerDashboardSection section)
        {
            return $"{PlayerDashboard}/{section.ToSlug()}";
        }

        public static string TeamPublicHome(string slug)
        {
            return $"/team/{slug}";
        }

        public static string TeamPublicHomeTab(string slug, SoccerTeamPublicTab tab)
        {
            return $"/team/{slug}/{tab.ToSlug()}";
        }
    }
}
