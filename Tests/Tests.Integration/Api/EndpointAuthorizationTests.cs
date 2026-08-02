using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;
using PlayGround.Server.Controllers.Auth;

namespace PlayGround.Tests.Integration.Api
{
    /// <summary>
    /// **인가를 빠뜨린 엔드포인트를 막는다.**
    ///
    /// 대부분의 컨트롤러는 클래스에 `[Authorize]`를 걸고 공개 액션만 `[AllowAnonymous]`로 여는데,
    /// `AuthController`·`SoccerLandingController`·`SoccerExportController`는 **클래스 수준 가드가 없다.**
    /// 이런 컨트롤러에 액션을 추가하면서 `[Authorize]`를 빠뜨리면 **아무 경고 없이 공개된다.**
    ///
    /// 규칙: 익명으로 열리는 액션은 아래 <see cref="IntentionallyPublic"/>에 **명시**돼야 한다.
    /// 새로 공개하려면 목록에 줄을 추가하게 만들어, 실수가 아니라 결정이 되게 한다.
    ///
    /// 한계: 특성(attribute)만 본다. 미들웨어·라우팅까지 태우는 검증은 `WebApplicationFactory`가
    /// 필요하고, 그건 `Microsoft.AspNetCore.Mvc.Testing` 패키지가 있어야 한다(Testing.md §8).
    /// </summary>
    public class EndpointAuthorizationTests
    {
        /// <summary>의도적으로 익명인 액션 — `{컨트롤러}.{액션}`.
        /// **여기 넣기 전에 "비로그인 아무나 봐도 되는 데이터인가"를 확인한다.**</summary>
        private static readonly HashSet<string> IntentionallyPublic = new(StringComparer.Ordinal)
        {
            // 로그인 자체는 인증 전이다 (이메일 로그인·소셜 시작·콜백)
            "AuthController.LoginByEmailAsync",
            "AuthController.SocialStart",
            "AuthController.SocialCallbackAsync",

            // 랜딩 — 비로그인 방문자가 보는 첫 화면
            "SoccerLandingController.GetContentsAsync",

            // 내려받기 링크는 토큰이 곧 인증이다(메일로 받은 1회용 URL)
            "SoccerExportController.DownloadAsync",

            // 링크 공유 미리보기(OG) 이미지 — 크롤러가 인증 없이 가져간다
            "OgImageController.Brand",
            "OgImageController.Team",
            "OgImageController.Tournament",

            // 공개 경기기록 — 서비스의 공개 축(DECISION.RECORDS)
            "SoccerRecordsController.GetTournamentsAsync",
            "SoccerRecordsController.GetTournamentDetailAsync",
            "SoccerRecordsController.GetMatchDetailAsync",

            // 공개 선수 프로필 — 보호자가 켠 공개 범위만 내려간다(서버가 비공개 항목을 null로 자른다)
            "SoccerPlayerController.GetPublicProfileAsync",

            // 팀 탐색 + 슬러그 기반 공개 팀 홈 6탭 — 비로그인 열람이 설계다
            "SoccerTeamController.GetExploreTeamsAsync",
            "SoccerTeamController.GetTeamHomeAsync",
            "SoccerTeamController.GetTeamNewsAsync",
            "SoccerTeamController.GetTeamSchedulesAsync",
            "SoccerTeamController.GetTeamScheduleFeedAsync",   // iCal 구독 — 공개 일정만
            "SoccerTeamController.GetTeamRecruitmentsAsync",
            "SoccerTeamController.GetTeamReviewsAsync",
            "SoccerTeamController.GetTeamCareerOutcomesAsync",
            "SoccerTeamController.GetTeamSeasonRecordAsync",
        };

        public static TheoryData<string> Actions
        {
            get
            {
                var data = new TheoryData<string>();
                foreach ((string key, _) in FindActions())
                {
                    data.Add(key);
                }

                return data;
            }
        }

        [Fact]
        public void 컨트롤러_액션이_하나_이상_발견된다()
        {
            // 리플렉션 탐색이 조용히 0건이 되면 아래 테스트가 전부 통과처럼 보인다
            FindActions().Should().NotBeEmpty();
        }

        [Fact]
        public void 익명으로_열린_액션은_공개_목록에_있어야_한다()
        {
            List<string> unguarded = FindActions()
                .Where(a => !IsGuarded(a.Method))
                .Select(a => a.Key)
                .Where(key => !IntentionallyPublic.Contains(key))
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

            unguarded.Should().BeEmpty(
                "인가가 빠졌거나, 의도한 공개라면 IntentionallyPublic 에 추가해야 한다");
        }

        [Fact]
        public void 공개_목록에_죽은_항목이_없다()
        {
            // 액션을 지웠는데 목록에 남으면, 다음에 같은 이름이 생겼을 때 조용히 통과한다
            HashSet<string> anonymous = FindActions()
                .Where(a => !IsGuarded(a.Method))
                .Select(a => a.Key)
                .ToHashSet(StringComparer.Ordinal);

            IntentionallyPublic.Except(anonymous)
                .Should().BeEmpty("이제 없거나 인가가 걸린 액션이다 — 목록에서 지운다");
        }

        [Theory]
        [MemberData(nameof(Actions))]
        public void 액션은_인가되거나_명시적으로_공개된다(string key)
        {
            // 위 집계 테스트는 어디가 틀렸는지 한 줄로 보여주지만, 액션별로도 남겨
            // 실패 목록이 곧 "가드 없는 엔드포인트 목록"이 되게 한다.
            (string Key, MethodInfo Method) action = FindActions().First(a => a.Key == key);

            bool allowed = IsGuarded(action.Method) || IntentionallyPublic.Contains(key);

            allowed.Should().BeTrue($"{key} 에 [Authorize]가 없다");
        }

        //.// 리플렉션 — 컨트롤러 액션과 유효 인가

        private static List<(string Key, MethodInfo Method)> FindActions()
        {
            var actions = new List<(string, MethodInfo)>();

            foreach (Type controller in ServerAssembly().GetTypes()
                .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
                .OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                foreach (MethodInfo method in controller
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                    .OrderBy(m => m.Name, StringComparer.Ordinal))
                {
                    actions.Add(($"{controller.Name}.{method.Name}", method));
                }
            }

            return actions;
        }

        private static Assembly ServerAssembly() => typeof(AuthController).Assembly;

        /// <summary>MVC의 판정과 같은 순서 — 액션의 `[AllowAnonymous]`가 클래스 `[Authorize]`를 이긴다.</summary>
        private static bool IsGuarded(MethodInfo action)
        {
            if (action.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null)
            {
                return false;
            }

            if (action.GetCustomAttribute<AuthorizeAttribute>(inherit: true) is not null)
            {
                return true;
            }

            Type controller = action.DeclaringType!;
            if (controller.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null)
            {
                return false;
            }

            return controller.GetCustomAttribute<AuthorizeAttribute>(inherit: true) is not null;
        }
    }
}
