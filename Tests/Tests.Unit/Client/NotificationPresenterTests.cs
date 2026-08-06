using FluentAssertions;
using PlayGround.Shared.Time;
using PlayGround.Client.Services;
using Xunit;
using PlayGround.Contracts.Notification;
using PlayGround.Domain.Soccer;
using PlayGround.Client.Components.Shared.Notifications;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>알림 표시 판정·딥링크. 벨 패널과 알림 센터가 같은 결과를 써야 하므로 여기 한 곳에서만 조립한다.
    /// 딥링크가 틀리면 사용자가 엉뚱한 화면에 떨어지고, 액션 판정이 틀리면 처리 필요 카운트가 어긋난다.</summary>
    [Collection(LocalizationCollection.Name)]
    public class NotificationPresenterTests
    {
        private static NotificationDto Item(SoccerNotificationType type, string? requestStatus = null,
            Guid? targetPlayerId = null, string? subText = null) =>
            new()
            {
                Type = type.ToString(),
                RequestStatus = requestStatus,
                TargetPlayerId = targetPlayerId,
                SubText = subText,
                RefId = Guid.NewGuid(),
            };

        //.// 에이전트 축 — flag OFF면 숨겨야 하는 유형

        [Theory]
        [InlineData(SoccerNotificationType.ViewRequest, true)]
        [InlineData(SoccerNotificationType.AgentGrantExpiring, true)]
        [InlineData(SoccerNotificationType.ClaimRequest, false)]
        [InlineData(SoccerNotificationType.MatchResult, false)]
        [InlineData(SoccerNotificationType.TeamNotice, false)]
        public void IsAgentType_에이전트_유형만_참이다(SoccerNotificationType type, bool expected)
        {
            NotificationPresenter.IsAgentType(Item(type)).Should().Be(expected);
        }

        //.// 액션형 — 인라인 처리 버튼이 붙는 유형

        [Theory]
        [InlineData(SoccerNotificationType.ClaimRequest, true)]
        [InlineData(SoccerNotificationType.RosterInvite, true)]
        [InlineData(SoccerNotificationType.ClaimApproved, false)]
        [InlineData(SoccerNotificationType.ExportReady, false)]
        public void IsActionType_인라인_처리_유형만_참이다(SoccerNotificationType type, bool expected)
        {
            NotificationPresenter.IsActionType(Item(type)).Should().Be(expected);
        }

        [Theory]
        [InlineData("Pending", true)]
        [InlineData("Approved", false)]
        [InlineData("Rejected", false)]
        [InlineData(null, false)]
        public void IsActionRequired_연결요청은_Pending일_때만_처리_필요다(string? status, bool expected)
        {
            NotificationPresenter.IsActionRequired(Item(SoccerNotificationType.ClaimRequest, status))
                .Should().Be(expected);
        }

        [Theory]
        [InlineData("Confirmed", false)]
        [InlineData("Pending", true)]
        [InlineData(null, true)]        // 미확인 상태 — 초대 확인 버튼이 남는다
        public void IsActionRequired_선수단초대는_확인_전까지_처리_필요다(string? status, bool expected)
        {
            NotificationPresenter.IsActionRequired(Item(SoccerNotificationType.RosterInvite, status))
                .Should().Be(expected);
        }

        [Fact]
        public void IsActionRequired_이동형은_언제나_거짓이다()
        {
            NotificationPresenter.IsActionRequired(Item(SoccerNotificationType.MatchResult, "Pending"))
                .Should().BeFalse();
        }

        //.// 딥링크

        [Fact]
        public void RouteOf_연결_승인은_해당_자녀_대시보드로_보낸다()
        {
            var player = Guid.NewGuid();

            string? route = NotificationPresenter.RouteOf(Item(SoccerNotificationType.ClaimApproved, targetPlayerId: player));

            route.Should().Contain($"playerId={player}");
        }

        [Fact]
        public void RouteOf_자녀_정보가_없으면_링크를_만들지_않는다()
        {
            // 링크에 넣을 대상이 없는데 이동시키면 빈 화면으로 떨어진다
            NotificationPresenter.RouteOf(Item(SoccerNotificationType.ClaimApproved)).Should().BeNull();
            NotificationPresenter.RouteOf(Item(SoccerNotificationType.MatchResult)).Should().BeNull();
            NotificationPresenter.RouteOf(Item(SoccerNotificationType.TeamNotice)).Should().BeNull();
        }

        [Fact]
        public void RouteOf_경기결과는_자녀_시즌통계로_보낸다()
        {
            var player = Guid.NewGuid();

            string? route = NotificationPresenter.RouteOf(Item(SoccerNotificationType.MatchResult, targetPlayerId: player));

            route.Should().Contain($"playerId={player}");
            route.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void RouteOf_열람_요청과_만료_임박은_같은_심사_화면으로_간다()
        {
            NotificationDto request = Item(SoccerNotificationType.ViewRequest);
            NotificationDto expiring = Item(SoccerNotificationType.AgentGrantExpiring);
            expiring.RefId = request.RefId;

            NotificationPresenter.RouteOf(expiring).Should().Be(NotificationPresenter.RouteOf(request));
        }

        [Fact]
        public void RouteOf_액션형은_이동_링크가_없다()
        {
            // 인라인으로 처리하는 유형이라 클릭 이동이 없어야 한다
            NotificationPresenter.RouteOf(Item(SoccerNotificationType.ClaimRequest)).Should().BeNull();
            NotificationPresenter.RouteOf(Item(SoccerNotificationType.RosterInvite)).Should().BeNull();
        }

        [Fact]
        public void RouteOf_수정신청_심사결과는_팀_경기결과_탭으로_간다()
        {
            NotificationPresenter.RouteOf(Item(SoccerNotificationType.CorrectionReviewed))
                .Should().NotBeNullOrEmpty();
        }

        //.// 상대 시각

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        public void TimeAgo_1분_미만은_방금_전이다(int seconds)
        {
            string label = NotificationPresenter.TimeAgo(SystemTime.Now.AddSeconds(-seconds));

            label.Should().Be(NotificationPresenter.TimeAgo(SystemTime.Now));
        }

        [Fact]
        public void TimeAgo_구간마다_다른_문구를_준다()
        {
            SystemTime now = SystemTime.Now;
            string justNow = NotificationPresenter.TimeAgo(now);
            string minutes = NotificationPresenter.TimeAgo(now.AddMinutes(-30));
            string hours = NotificationPresenter.TimeAgo(now.AddHours(-5));
            string yesterday = NotificationPresenter.TimeAgo(now.AddHours(-30));
            string older = NotificationPresenter.TimeAgo(now.AddDays(-10));

            new[] { justNow, minutes, hours, yesterday, older }.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void TimeAgo_48시간이_넘으면_날짜로_바뀐다()
        {
            SystemTime past = SystemTime.Now.AddDays(-10);

            string label = NotificationPresenter.TimeAgo(past);

            // 로컬 시각 기준 월/일 — 상대 표현이 아니어야 한다
            DateTime local = past.ToWallClock();
            label.Should().Contain(local.Month.ToString()).And.Contain(local.Day.ToString());
        }
    }
}
