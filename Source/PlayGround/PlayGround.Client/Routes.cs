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
        public const string TeamExplore = "/teams";
        public const string Claim = "/claim";
        public const string AgentApprovalTemplate = "/approvals/agent/{RequestId:guid}";
        public const string Settings = "/settings";
        public const string SettingsSectionTemplate = "/settings/{Section}";
        public const string Records = "/records";
        public const string RecordsArchive = "/records/archive";
        public const string RecordsDetailTemplate = "/records/{TournamentId:guid}";
        public const string NotFound = "/not-found";

        /// <summary>권한 없음 — 로그인 상태 전용(게스트는 로그인으로 리다이렉트).</summary>
        public const string Forbidden = "/forbidden";

        /// <summary>서버 오류 — 전역 예외 경계가 여기로 보낸다.</summary>
        public const string ServerError = "/error";

        /// <summary>공용 폼 컴포넌트 데모 (Design.FormPatterns 검증용 — 개발 전용).</summary>
        public const string DevFormPatterns = "/dev/form-patterns";

        /// <summary>토스트·확인 모달 데모 (Design.FeedbackPatterns 검증용 — 개발 전용).</summary>
        public const string DevFeedbackPatterns = "/dev/feedback-patterns";

        /// <summary>전역 예외 경계 확인용 — 예외를 던져 500 화면으로 가는지 본다 (개발 전용).</summary>
        public const string DevThrow = "/dev/throw";

        //.// 링크 생성 헬퍼 (파라미터 라우트)

        public static string TeamDashboardSection(SoccerTeamDashboardSection section)
        {
            return $"{TeamDashboard}/{section.ToSlug()}";
        }

        public static string PlayerDashboardSection(SoccerPlayerDashboardSection section)
        {
            return $"{PlayerDashboard}/{section.ToSlug()}";
        }

        public static string AgentApproval(Guid requestId)
        {
            return $"/approvals/agent/{requestId}";
        }

        public static string SettingsSection(SettingsSection section)
        {
            return $"{Settings}/{section.ToSlug()}";
        }

        public static string RecordsDetail(Guid tournamentId)
        {
            return $"{Records}/{tournamentId}";
        }

        public static string TeamPublicHome(string slug)
        {
            return $"/team/{slug}";
        }

        public static string TeamPublicHomeTab(string slug, SoccerTeamPublicTab tab)
        {
            return $"/team/{slug}/{tab.ToSlug()}";
        }

        /// <summary>
        /// 로그인 진입 — 항상 returnUrl을 보존한다(Design.Navigation 인증 플로우 1).
        /// returnUrl은 앱 내부 상대 경로만 허용 (외부 도메인으로 튕기는 오픈 리다이렉트 방지).
        /// </summary>
        public static string LoginWithReturn(string? returnUrl)
        {
            if (!IsSafeReturnUrl(returnUrl))
            {
                return Login;
            }

            return $"{Login}?returnUrl={Uri.EscapeDataString(returnUrl!)}";
        }

        /// <summary>"/team/abc" 형태의 내부 경로만 true. "//evil.com"·"http://…"는 거부.</summary>
        public static bool IsSafeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return false;
            }

            return returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal);
        }
    }
}
