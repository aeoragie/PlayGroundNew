using FluentAssertions;
using PlayGround.Shared.Time;
using PlayGround.Client.Services;
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
        public void HomeScoreLabel_ShowsDash_WhenNotFinished()
        {
            RecordsFormatting.HomeScoreLabel(Match(null, null)).Should().Be("-");
            RecordsFormatting.AwayScoreLabel(Match(null, null)).Should().Be("-");
        }

        [Fact]
        public void ScoreLabel_ShowsDigitsOnly_WithoutPenalties()
        {
            RecordsFormatting.HomeScoreLabel(Match(2, 1)).Should().Be("2");
            RecordsFormatting.AwayScoreLabel(Match(2, 1)).Should().Be("1");
        }

        [Fact]
        public void ScoreLabel_AppendsPenaltiesInParentheses()
        {
            // 홈은 뒤에, 원정은 앞에 — 스코어보드가 가운데를 향해 마주 본다
            RecordsMatchDto match = Match(1, 1, homePk: 4, awayPk: 3);

            RecordsFormatting.HomeScoreLabel(match).Should().Be("1 (4)");
            RecordsFormatting.AwayScoreLabel(match).Should().Be("(3) 1");
        }

        //.// 승자 판정 — 정규시간 우선, 동점이면 PK

        [Fact]
        public void Winner_IgnoresPenalties_WhenRegulationDecides()
        {
            RecordsMatchDto match = Match(3, 1, homePk: 0, awayPk: 9);

            RecordsFormatting.IsHomeWinner(match).Should().BeTrue();
            RecordsFormatting.IsAwayWinner(match).Should().BeFalse();
        }

        [Fact]
        public void Winner_UsesPenalties_WhenRegulationTied()
        {
            RecordsMatchDto match = Match(1, 1, homePk: 4, awayPk: 3);

            RecordsFormatting.IsHomeWinner(match).Should().BeTrue();
            RecordsFormatting.IsAwayWinner(match).Should().BeFalse();

            RecordsMatchDto awayWon = Match(1, 1, homePk: 3, awayPk: 4);
            RecordsFormatting.IsHomeWinner(awayWon).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(awayWon).Should().BeTrue();
        }

        [Fact]
        public void Winner_NeitherSide_OnDraw()
        {
            RecordsMatchDto draw = Match(1, 1);

            RecordsFormatting.IsHomeWinner(draw).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(draw).Should().BeFalse();
        }

        [Fact]
        public void Winner_NeitherSide_WhenNotFinished()
        {
            RecordsFormatting.IsHomeWinner(Match(null, null)).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(Match(null, null)).Should().BeFalse();
            RecordsFormatting.IsHomeWinner(Match(2, null)).Should().BeFalse();
            RecordsFormatting.IsAwayWinner(Match(null, 2)).Should().BeFalse();
        }

        [Fact]
        public void Winner_AwayWin_IsExclusiveWithHomeWin()
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
        public void IsGoalEvent_BallIcon_OnlyForScoringTypes(string eventType, bool expected)
        {
            RecordsFormatting.IsGoalEvent(eventType).Should().Be(expected);
        }

        [Fact]
        public void EventKindLabel_IsEmpty_ForUnknownType()
        {
            // 타임라인에 정체불명의 문자열이 뜨지 않게 한다
            RecordsFormatting.EventKindLabel("Substitution").Should().BeEmpty();
            RecordsFormatting.EventKindLabel("").Should().BeEmpty();
        }

        [Fact]
        public void EventKindLabel_OwnGoal_DiffersFromGoal()
        {
            RecordsFormatting.EventKindLabel("OwnGoal")
                .Should().NotBe(RecordsFormatting.EventKindLabel("Goal"));
        }

        [Fact]
        public void EventLogText_ShowsKindOnly_WithoutPlayerName()
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
        public void RoundDisplay_IsEmpty_WithoutRound(string? round, string expected)
        {
            RecordsFormatting.RoundDisplay(round).Should().Be(expected);
        }

        [Fact]
        public void RoundDisplay_RNumber_BecomesGroupRound()
        {
            RecordsFormatting.RoundDisplay("R1").Should().Contain("1");
            RecordsFormatting.RoundDisplay("R3").Should().Contain("3");
        }

        [Fact]
        public void RoundDisplay_KnockoutAbbreviations_HaveDistinctLabels()
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
        public void RoundDisplay_ShowsRawValue_WhenUnknown()
        {
            // 대회마다 라운드 표기가 달라 임의 문자열이 올 수 있다 — 삼키지 않는다
            RecordsFormatting.RoundDisplay("플레이인").Should().Be("플레이인");
        }

        [Fact]
        public void MatchStageLabel_ShowsStageAndRoundTogether()
        {
            string groupWithRound = RecordsFormatting.MatchStageLabel("Group", "R1");
            string groupOnly = RecordsFormatting.MatchStageLabel("Group", null);

            groupWithRound.Should().StartWith(groupOnly).And.NotBe(groupOnly);
        }

        [Fact]
        public void MatchStageLabel_KnockoutAndLeague_ShowRoundOnly()
        {
            RecordsFormatting.MatchStageLabel("Knockout", "F")
                .Should().Be(RecordsFormatting.RoundDisplay("F"));
            RecordsFormatting.MatchStageLabel("League", "R5")
                .Should().Be(RecordsFormatting.RoundDisplay("R5"));
        }

        [Fact]
        public void MatchStageLabel_FallsBackToRound_ForUnknownStage()
        {
            RecordsFormatting.MatchStageLabel("Unknown", "R2")
                .Should().Be(RecordsFormatting.RoundDisplay("R2"));
        }

        //.// 일시

        [Fact]
        public void WhenLabel_AppendsWeekdayAndTime()
        {
            // 표시는 DisplayTime 규칙(한국 시간 고정)이다 — 6/7 10:00 KST = 6/7 01:00 UTC.
            // 기대값이 머신 시간대와 무관하게 결정적이다.
            var utc = new SystemTime(2026, 6, 7, 1, 0, 0); // 일요일

            string label = RecordsFormatting.WhenLabel(utc);

            label.Should().StartWith("6/7 (").And.EndWith("10:00");
        }

        [Fact]
        public void WhenLabel_WeekdayLetter_DiffersPerDay()
        {
            var week = Enumerable.Range(0, 7)
                .Select(i => RecordsFormatting.WhenLabel(new SystemTime(2026, 6, 7).AddDays(i)))
                .ToArray();

            week.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void WhenLabel_HasOwnText_WhenScheduleTbd()
        {
            RecordsFormatting.WhenLabel(null).Should().NotBeNullOrWhiteSpace();
            RecordsFormatting.WhenLabel(null).Should().NotContain("/");
        }
    }
}
