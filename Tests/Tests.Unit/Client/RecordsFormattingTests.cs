using FluentAssertions;
using Xunit;
using PlayGround.Contracts.Records;
using PlayGround.Client.Components.Records;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>공개 경기기록 표시 파생 — 스코어·PK·승자 판정. 문구가 아니라 **구조와 판정**을 고정한다
    /// (문구 자체는 LocalizationResourceTests가 리소스로 검증한다).</summary>
    [Collection(LocalizationCollection.Name)]
    public class RecordsFormattingTests
    {
        private static RecordsMatchDto Match(int? home, int? away, int? homePk = null, int? awayPk = null) =>
            new() { HomeScore = home, AwayScore = away, HomePkScore = homePk, AwayPkScore = awayPk };

        //.// 스코어 표기

        [Fact]
        public void HomeScoreLabel_미종료는_대시다()
        {
            RecordsFormatting.HomeScoreLabel(Match(null, null)).Should().Be("-");
            RecordsFormatting.AwayScoreLabel(Match(null, null)).Should().Be("-");
        }

        [Fact]
        public void 스코어_라벨은_PK가_없으면_숫자만_보인다()
        {
            RecordsFormatting.HomeScoreLabel(Match(2, 1)).Should().Be("2");
            RecordsFormatting.AwayScoreLabel(Match(2, 1)).Should().Be("1");
        }

        [Fact]
        public void 스코어_라벨은_PK를_괄호로_붙인다()
        {
            // 홈은 뒤에, 원정은 앞에 — 스코어보드가 가운데를 향해 마주 본다
            RecordsMatchDto match = Match(1, 1, homePk: 4, awayPk: 3);

            RecordsFormatting.HomeScoreLabel(match).Should().Be("1 (4)");
            RecordsFormatting.AwayScoreLabel(match).Should().Be("(3) 1");
        }

        //.// 승자 판정 — 정규시간 우선, 동점이면 PK

        [Fact]
        public void 승자판정_정규시간에서_갈리면_PK는_보지_않는다()
        {
            RecordsMatchDto match = Match(3, 1, homePk: 0, awayPk: 9);

            RecordsFormatting.IsHomeWinner(match).Should().BeTrue();
            RecordsFormatting.IsAwayWinner(match).Should().BeFalse();
        }

        [Fact]
        public void 승자판정_정규시간_동점이면_PK로_가린다()
        {
            RecordsMatchDto match = Match(1, 1, homePk: 4, awayPk: 3);

            RecordsFormatting.IsHomeWinner(match).Should().BeTrue();
            RecordsFormatting.IsAwayWinner(match).Should().BeFalse();

            RecordsMatchDto awayWon = Match(1, 1, homePk: 3, awayPk: 4);
            RecordsFormatting.IsHomeWinner(awayWon).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(awayWon).Should().BeTrue();
        }

        [Fact]
        public void 승자판정_무승부는_양쪽_모두_승자가_아니다()
        {
            RecordsMatchDto draw = Match(1, 1);

            RecordsFormatting.IsHomeWinner(draw).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(draw).Should().BeFalse();
        }

        [Fact]
        public void 승자판정_미종료는_양쪽_모두_승자가_아니다()
        {
            RecordsFormatting.IsHomeWinner(Match(null, null)).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(Match(null, null)).Should().BeFalse();
            RecordsFormatting.IsHomeWinner(Match(2, null)).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(Match(null, 2)).Should().BeFalse();
        }

        [Fact]
        public void 승자판정_원정_승리는_홈_승리와_배타적이다()
        {
            RecordsMatchDto match = Match(0, 2);

            RecordsFormatting.IsHomeWinner(match).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(match).Should().BeTrue();
        }

        //.// 이벤트

        [Theory]
        [InlineData("Goal", true)]
        [InlineData("PenaltyGoal", true)]
        [InlineData("OwnGoal", true)]
        [InlineData("YellowCard", false)]
        [InlineData("RedCard", false)]
        [InlineData("Substitution", false)]
        public void IsGoalEvent_득점형만_공_아이콘이다(string eventType, bool expected)
        {
            RecordsFormatting.IsGoalEvent(eventType).Should().Be(expected);
        }

        [Fact]
        public void EventKindLabel_알_수_없는_유형은_빈_문자열이다()
        {
            // 타임라인에 정체불명의 문자열이 뜨지 않게 한다
            RecordsFormatting.EventKindLabel("Substitution").Should().BeEmpty();
            RecordsFormatting.EventKindLabel("").Should().BeEmpty();
        }

        [Fact]
        public void EventKindLabel_자책골은_득점과_다른_라벨이다()
        {
            RecordsFormatting.EventKindLabel("OwnGoal")
                .Should().NotBe(RecordsFormatting.EventKindLabel("Goal"));
        }

        [Fact]
        public void EventLogText_선수명이_없으면_종류만_보인다()
        {
            var withoutName = new RecordsMatchEventDto { EventType = "Goal", PlayerName = null };
            var withName = new RecordsMatchEventDto { EventType = "Goal", PlayerName = "김유한" };

            RecordsFormatting.EventLogText(withoutName).Should().Be(RecordsFormatting.EventKindLabel("Goal"));
            RecordsFormatting.EventLogText(withName).Should().Contain("김유한");
        }

        //.// 라운드 표기

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void RoundDisplay_라운드가_없으면_빈_문자열이다(string? round, string expected)
        {
            RecordsFormatting.RoundDisplay(round).Should().Be(expected);
        }

        [Fact]
        public void RoundDisplay_R숫자는_조별_라운드로_바뀐다()
        {
            RecordsFormatting.RoundDisplay("R1").Should().Contain("1");
            RecordsFormatting.RoundDisplay("R3").Should().Contain("3");
        }

        [Fact]
        public void RoundDisplay_토너먼트_약어는_각각_다른_라벨이다()
        {
            string[] labels =
            [
                RecordsFormatting.RoundDisplay("PO"),
                RecordsFormatting.RoundDisplay("R16"),
                RecordsFormatting.RoundDisplay("QF"),
                RecordsFormatting.RoundDisplay("SF"),
                RecordsFormatting.RoundDisplay("F"),
            ];

            labels.Should().OnlyHaveUniqueItems();
            labels.Should().NotContain(string.Empty);
        }

        [Fact]
        public void RoundDisplay_모르는_값은_원문을_그대로_보여준다()
        {
            // 대회마다 라운드 표기가 달라 임의 문자열이 올 수 있다 — 삼키지 않는다
            RecordsFormatting.RoundDisplay("플레이인").Should().Be("플레이인");
        }

        [Fact]
        public void MatchStageLabel_스테이지와_라운드를_함께_보여준다()
        {
            string groupWithRound = RecordsFormatting.MatchStageLabel("Group", "R1");
            string groupOnly = RecordsFormatting.MatchStageLabel("Group", null);

            groupWithRound.Should().StartWith(groupOnly).And.NotBe(groupOnly);
        }

        [Fact]
        public void MatchStageLabel_토너먼트와_리그는_라운드가_있으면_라운드만_보여준다()
        {
            RecordsFormatting.MatchStageLabel("Knockout", "F")
                .Should().Be(RecordsFormatting.RoundDisplay("F"));
            RecordsFormatting.MatchStageLabel("League", "R5")
                .Should().Be(RecordsFormatting.RoundDisplay("R5"));
        }

        [Fact]
        public void MatchStageLabel_모르는_스테이지는_라운드로_떨어진다()
        {
            RecordsFormatting.MatchStageLabel("Unknown", "R2")
                .Should().Be(RecordsFormatting.RoundDisplay("R2"));
        }

        //.// 일시

        [Fact]
        public void WhenLabel_요일과_시각을_붙인다()
        {
            var sunday = new DateTime(2026, 6, 7, 10, 0, 0); // 일요일

            string label = RecordsFormatting.WhenLabel(sunday);

            label.Should().StartWith("6/7 (").And.EndWith("10:00");
        }

        [Fact]
        public void WhenLabel_요일_글자는_요일마다_다르다()
        {
            var week = Enumerable.Range(0, 7)
                .Select(i => RecordsFormatting.WhenLabel(new DateTime(2026, 6, 7).AddDays(i)))
                .ToArray();

            week.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void WhenLabel_일시_미정은_별도_문구다()
        {
            RecordsFormatting.WhenLabel(null).Should().NotBeNullOrWhiteSpace();
            RecordsFormatting.WhenLabel(null).Should().NotContain("/");
        }
    }
}
