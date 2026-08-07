using FluentAssertions;
using Xunit;
using PlayGround.Client.Models;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>공식/친선 세그먼트 축. **집계는 Official만** — 친선이 순위표·시즌 요약에 섞이면
    /// 공식 기록의 신뢰가 깨진다(설계 결정 7). 필터 판정과 URL 왕복을 고정한다.</summary>
    public class SoccerMatchSegmentTests
    {
        //.// 저장 문자열 → 경기 성격

        [Theory]
        [InlineData("Friendly", true)]
        [InlineData("Official", false)]
        [InlineData("Unknown", false)]   // 알 수 없으면 공식 — 다수가 공식이라 안전한 기본값
        [InlineData(null, false)]
        [InlineData("", false)]
        public void IsFriendly_ParsesStoredString(string? value, bool expected)
        {
            SoccerMatchTypeExtensions.IsFriendly(value).Should().Be(expected);
        }

        [Fact]
        public void Parse_FallsBackToOfficial_WhenUnknown()
        {
            SoccerMatchTypeExtensions.Parse("이상한값").Should().Be(SoccerMatchType.Official);
            SoccerMatchTypeExtensions.Parse(null).Should().Be(SoccerMatchType.Official);
        }

        //.// 세그먼트 필터 — 목록에 무엇이 남는가

        [Theory]
        [InlineData(SoccerMatchSegment.All, "Official", true)]
        [InlineData(SoccerMatchSegment.All, "Friendly", true)]
        [InlineData(SoccerMatchSegment.All, null, true)]
        [InlineData(SoccerMatchSegment.Official, "Official", true)]
        [InlineData(SoccerMatchSegment.Official, "Friendly", false)]
        [InlineData(SoccerMatchSegment.Official, null, true)]      // 미상은 공식으로 본다
        [InlineData(SoccerMatchSegment.Friendly, "Friendly", true)]
        [InlineData(SoccerMatchSegment.Friendly, "Official", false)]
        [InlineData(SoccerMatchSegment.Friendly, null, false)]
        public void Matches_SegmentAcceptsMatch(SoccerMatchSegment segment, string? matchType, bool expected)
        {
            segment.Matches(matchType).Should().Be(expected);
        }

        [Fact]
        public void Matches_OfficialAndFriendly_PartitionAllMatches()
        {
            // 어느 경기도 두 세그먼트에서 동시에 보이거나 둘 다에서 사라지면 안 된다
            foreach (string? matchType in new[] { "Official", "Friendly", "Unknown", null })
            {
                bool official = SoccerMatchSegment.Official.Matches(matchType);
                bool friendly = SoccerMatchSegment.Friendly.Matches(matchType);

                (official ^ friendly).Should().BeTrue($"matchType={matchType ?? "null"}");
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
            SoccerMatchTypeExtensions.ParseSegment(segment.ToQuery()).Should().Be(segment);
        }

        [Theory]
        [InlineData("OFFICIAL", SoccerMatchSegment.Official)]   // 대소문자 흔들려도 받는다
        [InlineData("Friendly", SoccerMatchSegment.Friendly)]
        [InlineData("이상한값", SoccerMatchSegment.All)]
        [InlineData(null, SoccerMatchSegment.All)]
        [InlineData("", SoccerMatchSegment.All)]
        public void ParseSegment_FallsBackToAll_WhenUnknown(string? query, SoccerMatchSegment expected)
        {
            SoccerMatchTypeExtensions.ParseSegment(query).Should().Be(expected);
        }
    }
}
