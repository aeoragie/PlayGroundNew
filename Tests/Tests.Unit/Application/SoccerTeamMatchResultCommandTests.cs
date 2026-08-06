using FluentAssertions;
using Moq;
using Xunit;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using PlayGround.Contracts.Team;
using PlayGround.Domain.Account;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Team.Commands;

namespace PlayGround.Tests.Unit.Application
{
    /// <summary>친선경기 결과 입력 — 팀이 입력하는 경기는 항상 친선이다(설계 결정 7).
    /// 검증은 클라이언트 인라인 규칙과 같아야 하고, 보호자 알림은 수신 설정을 존중해야 한다.</summary>
    public class SoccerTeamMatchResultCommandTests
    {
        private static readonly Guid Manager = Guid.NewGuid();

        private static CreateTeamMatchResultRequest Request(
            string opponent = " 강동 SC ", int ours = 3, int theirs = 1, SystemTime? at = null, string? venue = "  한강 구장  ") =>
            new()
            {
                OpponentName = opponent,
                OurScore = ours,
                OpponentScore = theirs,
                MatchedAt = at ?? SystemTime.Now.AddDays(-1),
                VenueName = venue,
            };

        private sealed class Harness
        {
            public Mock<ISoccerTeamRepository> Team { get; } = new();
            public Mock<INotificationRepository> Notifications { get; } = new();
            public Mock<IAccountRepository> Accounts { get; } = new();
            public List<Guid> Notified { get; } = new();

            public Harness(Guid? savedMatchId = null, List<NotificationRecipient>? recipients = null,
                Dictionary<Guid, bool>? states = null)
            {
                Team.Setup(r => r.CreateMatchResultByManagerAsync(It.IsAny<Guid>(), It.IsAny<CreateTeamMatchResultRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Guid?>.Success(savedMatchId ?? Guid.NewGuid()));
                Notifications.Setup(r => r.GetMatchResultRecipientsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<List<NotificationRecipient>>.Success(recipients ?? new List<NotificationRecipient>()));
                Accounts.Setup(r => r.GetNotificationStatesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Dictionary<Guid, bool>>.Success(states ?? new Dictionary<Guid, bool>()));
                Notifications.Setup(r => r.CreateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                        It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .Callback((Guid userId, string _, Guid _, Guid? _, string? _, string? _, string? _, string? _, string? _, CancellationToken _) => Notified.Add(userId))
                    .ReturnsAsync(Result<bool>.Success(true));
            }

            public SoccerTeamMatchResultCommand Command =>
                new(Team.Object, Notifications.Object, Accounts.Object);
        }

        private static NotificationRecipient Recipient(Guid userId) =>
            new() { UserId = userId, PlayerId = Guid.NewGuid(), PlayerName = "김유한", TeamName = "FC 한강" };

        //.// 인가·검증

        [Fact]
        public async Task ExecuteAsync_빈_사용자는_Unauthorized다()
        {
            Result<CreateTeamMatchResultResponse> result =
                await new Harness().Command.ExecuteAsync(Guid.Empty, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExecuteAsync_상대팀명이_비면_InvalidInput이다(string opponent)
        {
            Result<CreateTeamMatchResultResponse> result =
                await new Harness().Command.ExecuteAsync(Manager, Request(opponent: opponent));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(100, 0)]   // 상한 99 — 오타·자동입력 폭주 방어
        [InlineData(0, 100)]
        public async Task ExecuteAsync_점수가_범위를_벗어나면_InvalidInput이다(int ours, int theirs)
        {
            Result<CreateTeamMatchResultResponse> result =
                await new Harness().Command.ExecuteAsync(Manager, Request(ours: ours, theirs: theirs));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(99, 99)]   // 경계는 허용
        public async Task ExecuteAsync_경계_점수는_통과한다(int ours, int theirs)
        {
            Result<CreateTeamMatchResultResponse> result =
                await new Harness().Command.ExecuteAsync(Manager, Request(ours: ours, theirs: theirs));

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_일시가_없으면_InvalidInput이다()
        {
            Result<CreateTeamMatchResultResponse> result =
                await new Harness().Command.ExecuteAsync(Manager, Request(at: default(SystemTime)));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task ExecuteAsync_미래_경기의_결과는_거부한다()
        {
            Result<CreateTeamMatchResultResponse> result =
                await new Harness().Command.ExecuteAsync(Manager, Request(at: SystemTime.Now.AddDays(3)));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task ExecuteAsync_시간대_차이를_감안해_내일까지는_받는다()
        {
            // 요청의 MatchedAt은 UTC 순간이다. 판정은 한국 달력 기준이라
            // "한국 시각으로 내일"까지는 통과해야 한다(입력 시점의 시차를 감안한 여유).
            Result<CreateTeamMatchResultResponse> result =
                await new Harness().Command.ExecuteAsync(Manager, Request(at: SystemTime.Now.AddDays(1)));

            result.IsSuccess.Should().BeTrue();
        }

        //.// 정규화·저장 결과

        [Fact]
        public async Task ExecuteAsync_상대팀명과_구장명의_공백을_정리한다()
        {
            var harness = new Harness();
            CreateTeamMatchResultRequest request = Request();

            await harness.Command.ExecuteAsync(Manager, request);

            request.OpponentName.Should().Be("강동 SC");
            request.VenueName.Should().Be("한강 구장");
        }

        [Fact]
        public async Task ExecuteAsync_구장명이_공백뿐이면_null로_저장한다()
        {
            var harness = new Harness();
            CreateTeamMatchResultRequest request = Request(venue: "   ");

            await harness.Command.ExecuteAsync(Manager, request);

            request.VenueName.Should().BeNull();
        }

        [Fact]
        public async Task ExecuteAsync_팀을_못_찾으면_NotFound다()
        {
            var harness = new Harness();
            harness.Team.Setup(r => r.CreateMatchResultByManagerAsync(It.IsAny<Guid>(), It.IsAny<CreateTeamMatchResultRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Guid?>.Success(null));

            Result<CreateTeamMatchResultResponse> result = await harness.Command.ExecuteAsync(Manager, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        //.// 보호자 알림 — 수신 설정 존중

        [Fact]
        public async Task ExecuteAsync_설정이_없으면_기본값으로_발송한다()
        {
            // MatchResult 기본값은 켬 — 저장 행이 없는 계정도 받는다
            var user = Guid.NewGuid();
            var harness = new Harness(recipients: [Recipient(user)]);

            await harness.Command.ExecuteAsync(Manager, Request());

            harness.Notified.Should().Equal(user);
        }

        [Fact]
        public async Task ExecuteAsync_수신을_끈_계정에는_보내지_않는다()
        {
            var off = Guid.NewGuid();
            var on = Guid.NewGuid();
            var harness = new Harness(
                recipients: [Recipient(off), Recipient(on)],
                states: new Dictionary<Guid, bool> { [off] = false, [on] = true });

            await harness.Command.ExecuteAsync(Manager, Request());

            harness.Notified.Should().Equal(on);
        }

        [Fact]
        public async Task ExecuteAsync_알림_조회가_실패해도_경기_저장은_성공이다()
        {
            // 알림은 부가 작업 — 실패해도 결과 저장을 되돌리지 않는다
            var harness = new Harness();
            harness.Notifications.Setup(r => r.GetMatchResultRecipientsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<NotificationRecipient>>.Error(ErrorCode.DatabaseError));

            Result<CreateTeamMatchResultResponse> result = await harness.Command.ExecuteAsync(Manager, Request());

            result.IsSuccess.Should().BeTrue();
            harness.Notified.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_알림은_MatchResult_유형으로_스코어를_담아_보낸다()
        {
            var user = Guid.NewGuid();
            var harness = new Harness(recipients: [Recipient(user)]);

            await harness.Command.ExecuteAsync(Manager, Request(ours: 3, theirs: 1));

            harness.Notifications.Verify(r => r.CreateAsync(
                user,
                nameof(SoccerNotificationType.MatchResult),
                It.IsAny<Guid>(), It.IsAny<Guid?>(),
                "강동 SC", "김유한", "FC 한강",
                "3:1", null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_수신_설정은_MatchResult_항목으로_조회한다()
        {
            var harness = new Harness(recipients: [Recipient(Guid.NewGuid())]);

            await harness.Command.ExecuteAsync(Manager, Request());

            harness.Accounts.Verify(r => r.GetNotificationStatesAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                nameof(NotificationPreferenceItem.MatchResult),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
