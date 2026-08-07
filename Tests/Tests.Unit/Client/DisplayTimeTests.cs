using FluentAssertions;
using PlayGround.Client.Services;
using PlayGround.Shared.Time;
using Xunit;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>
    /// **표시 시간대의 단일 결정권자** — `DisplayTime`만 "이 사람에게 몇 시로 보여줄까"를 안다.
    ///
    /// 여기가 어긋나면 화면의 모든 날짜·시각이 조용히 밀린다. 특히 **날짜 경계**가 위험하다.
    /// 서울에서 UTC 15시 이후는 이미 다음 날이라, 변환을 빠뜨리면 "어제 일정"이 오늘로 보인다.
    ///
    /// 기본값은 브라우저 시간대라 머신마다 다르다 — 테스트는 <see cref="DisplayTime.Override"/>로
    /// 시간대를 고정해 **어디서 돌려도 결과가 같게** 만든다.
    /// </summary>
    public class DisplayTimeTests : IDisposable
    {
        private static readonly TimeZoneInfo Seoul = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
        private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        public DisplayTimeTests() => DisplayTime.Override = Seoul;

        public void Dispose() => DisplayTime.Override = null;

        [Fact]
        public void ToWallClock은_표시_시간대로_되돌린다()
        {
            new SystemTime(2026, 8, 10, 12, 30, 0).ToWallClock()
                .Should().Be(new DateTime(2026, 8, 10, 21, 30, 0));
        }

        [Fact]
        public void 벽시계는_Kind가_Unspecified다()
        {
            // Local·Utc로 표식하면 누가 ToUniversalTime()을 부르는 순간 오프셋이 샌다.
            // Override 유무에 따라 Kind가 달라지지 않도록 고정해 둔 계약이다.
            new SystemTime(2026, 8, 10, 12, 0, 0).ToWallClock().Kind
                .Should().Be(DateTimeKind.Unspecified);

            DisplayTime.Override = null;   // 브라우저 시간대(= 테스트 머신)일 때도 같아야 한다
            new SystemTime(2026, 8, 10, 12, 0, 0).ToWallClock().Kind
                .Should().Be(DateTimeKind.Unspecified);
        }

        [Fact]
        public void 날짜_경계가_그_시간대_자정에서_넘어간다()
        {
            // 서울에서 UTC 15:00 = 다음 날 00:00 — 변환을 빠뜨리면 하루 밀린다
            new SystemTime(2026, 8, 10, 14, 59, 0).ToWallClock().Day.Should().Be(10);
            new SystemTime(2026, 8, 10, 15, 0, 0).ToWallClock().Day.Should().Be(11);
        }

        [Fact]
        public void 표시_시간대가_다르면_다른_시각으로_보인다()
        {
            var moment = new SystemTime(2026, 8, 10, 15, 0, 0);

            moment.ToWallClock().Should().Be(new DateTime(2026, 8, 11, 0, 0, 0));   // 서울 +9

            DisplayTime.Override = NewYork;
            moment.ToWallClock().Should().Be(new DateTime(2026, 8, 10, 11, 0, 0));  // 뉴욕 -4 (여름)
        }

        [Fact]
        public void FromWallClock은_입력을_표시_시간대로_해석한다()
        {
            // 픽커가 준 "8/11 00:00"은 서울 시각이므로 8/10 15:00 UTC다
            SystemTime utc = DisplayTime.FromWallClock(new DateTime(2026, 8, 11, 0, 0, 0));

            utc.Should().Be(new SystemTime(2026, 8, 10, 15, 0, 0));
            utc.UtcDateTime.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        public void FromWallClock은_입력_Kind를_무시한다(DateTimeKind kind)
        {
            // 픽커가 어떤 Kind로 만들어 주든 "사용자가 화면에서 본 시각"이라는 뜻은 같다 —
            // 기기 설정에 따라 결과가 달라지면 안 된다
            var wall = new DateTime(2026, 8, 11, 0, 0, 0, kind);

            DisplayTime.FromWallClock(wall).Should().Be(new SystemTime(2026, 8, 10, 15, 0, 0));
        }

        [Fact]
        public void 존재하지_않는_시각을_입력해도_터지지_않는다()
        {
            // 서머타임 시작일에는 그 지역에 아예 없는 시각이 생긴다(뉴욕 3/8 02:30).
            // 픽커가 그걸 만들어 보낼 수 있고, 저장이 예외로 죽으면 안 된다.
            DisplayTime.Override = NewYork;
            var lostHour = new DateTime(2026, 3, 8, 2, 30, 0);
            NewYork.IsInvalidTime(lostHour).Should().BeTrue("이 테스트의 전제 — 실제로 없는 시각이어야 한다");

            Action act = () => DisplayTime.FromWallClock(lostHour);

            act.Should().NotThrow();
        }

        [Fact]
        public void 폼_왕복에서_값이_변하지_않는다()
        {
            // 일정 수정: 서버 값 → 픽커 → 저장. 한 번 돌 때마다 시각이 밀리면 안 된다
            var original = new SystemTime(2026, 8, 10, 15, 0, 0);

            DisplayTime.FromWallClock(original.ToWallClock()).Should().Be(original);
        }

        [Fact]
        public void Format은_벽시계로_문자열을_만든다()
        {
            new SystemTime(2026, 8, 10, 15, 0, 0).Format("M/d HH:mm").Should().Be("8/11 00:00");
        }
    }
}
