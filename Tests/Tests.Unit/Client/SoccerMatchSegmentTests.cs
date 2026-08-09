using FluentAssertions;
using PlayGround.Client.Models;
using PlayGround.Contracts.Soccer;
using Xunit;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>공식/친선 세그먼트 축. **집계는 Official만** — 친선이 순위표·시즌 요약에 섞이면
    /// 공식 기록의 신뢰가 깨진다(설계 결정 7). 필터 판정과 URL 왕복을 고정한다.</summary>
    public class SoccerMatchSegmentTests
    {
        //.// 세그먼트 필터 — 목록에 무엇이 남는가

        [Theory]
        [InlineData(SoccerMatchSegment.All, SoccerMatchType.Official, true)]
        [InlineData(SoccerMatchSegment.All, SoccerMatchType.Friendly, true)]
        [InlineData(SoccerMatchSegment.All, SoccerMatchType.Unknown, true)]
        [InlineData(SoccerMatchSegment.Official, SoccerMatchType.Official, true)]
        [InlineData(SoccerMatchSegment.Official, SoccerMatchType.Friendly, false)]
        [InlineData(SoccerMatchSegment.Official, SoccerMatchType.Unknown, true)]   // 미상은 공식으로 본다
        [InlineData(SoccerMatchSegment.Friendly, SoccerMatchType.Friendly, true)]
        [InlineData(SoccerMatchSegment.Friendly, SoccerMatchType.Official, false)]
        [InlineData(SoccerMatchSegment.Friendly, SoccerMatchType.Unknown, false)]
        public void Matches_SegmentAcceptsMatch(SoccerMatchSegment segment, SoccerMatchType matchType, bool expected)
        {
            segment.Matches(matchType).Should().Be(expected);
        }

        [Fact]
        public void Matches_OfficialAndFriendly_PartitionAllMatches()
        {
            // 어느 경기도 두 세그먼트에서 동시에 보이거나 둘 다에서 사라지면 안 된다
            foreach (SoccerMatchType matchType in Enum.GetValues<SoccerMatchType>())
            {
                bool official = SoccerMatchSegment.Official.Matches(matchType);
                bool friendly = SoccerMatchSegment.Friendly.Matches(matchType);

                (official ^ friendly).Should().BeTrue($"matchType={matchType}");
                SoccerMatchSegment.All.Matches(matchType).Should().BeTrue();
            }
        }

        //.// URL 왕복 — 필터가 공유·새로고침에서 살아남아야 한다

        [Fact]
        public void ToQuery_OmitsAll_BecauseItIsDefault()
        {
            SoccerMatchSegment.All.ToQuery().Should().BeNull();
            SoccerMatchSegment.Official.ToQuery().Should().Be("official");
            SoccerMatchSegment.Friendly.ToQuery().Should().Be("friendly");
        }

        [Theory]
        [InlineData(SoccerMatchSegment.All)]
        [InlineData(SoccerMatchSegment.Official)]
        [InlineData(SoccerMatchSegment.Friendly)]
        public void Segment_SurvivesUrlRoundTrip(SoccerMatchSegment segment)
        {
            SoccerMatchSegmentExtensions.ParseSegment(segment.ToQuery()).Should().Be(segment);
        }

        [Theory]
        [InlineData("OFFICIAL", SoccerMatchSegment.Official)]   // 대소문자 흔들려도 받는다
        [InlineData("Friendly", SoccerMatchSegment.Friendly)]
        [InlineData("이상한값", SoccerMatchSegment.All)]
        [InlineData(null, SoccerMatchSegment.All)]
        [InlineData("", SoccerMatchSegment.All)]
        public void ParseSegment_FallsBackToAll_WhenUnknown(string? query, SoccerMatchSegment expected)
        {
            SoccerMatchSegmentExtensions.ParseSegment(query).Should().Be(expected);
        }
    }
}
