using FluentAssertions;
using PlayGround.Client.Services;
using PlayGround.Shared.Time;
using Xunit;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>
    /// **표시 시간대의 단일 결정권자** — `DisplayTime`만 시간대를 안다(지금은 KST 고정).
    ///
    /// 여기가 어긋나면 화면의 모든 날짜·시각이 조용히 밀린다. 특히 **날짜 경계**가 위험하다.
    /// UTC 15시 이후는 한국에서 이미 다음 날이라, 변환을 빠뜨리면 "어제 일정"이 오늘로 보인다.
    ///
    /// 계정 시간대 설정이 생기면 이 클래스 안의 오프셋 결정만 바뀌고 호출부는 그대로다 —
    /// 그때 이 테스트가 계약이 유지되는지 확인해 준다.
    /// </summary>
    public class DisplayTimeTests
    {
        [Fact]
        public void ToWallClock은_UTC에_9시간을_더한다()
        {
            var utc = new SystemTime(2026, 8, 10, 12, 30, 0);

            DateTime wall = utc.ToWallClock();

            wall.Should().Be(new DateTime(2026, 8, 10, 21, 30, 0, DateTimeKind.Unspecified));
        }

        [Fact]
        public void 벽시계는_Kind가_Unspecified다()
        {
            // **UTC라고 표식하면 안 된다.** 값은 KST인데 Kind가 Utc면,
            // 누군가 ToUniversalTime()을 부르는 순간 아무 일도 안 일어나 9시간이 그대로 샌다.
            // 어느 시간대인지 모르는 벽시계 = Unspecified가 정직한 표식이다.
            new SystemTime(2026, 8, 10, 12, 0, 0).ToWallClock().Kind
                .Should().Be(DateTimeKind.Unspecified);
        }

        [Fact]
        public void 날짜_경계가_UTC_15시에서_넘어간다()
        {
            // 8/10 15:00 UTC = 8/11 00:00 KST — 변환을 빠뜨리면 하루 밀린다
            new SystemTime(2026, 8, 10, 14, 59, 0).ToWallClock().Day.Should().Be(10);
            new SystemTime(2026, 8, 10, 15, 0, 0).ToWallClock().Day.Should().Be(11);
        }

        [Fact]
        public void FromWallClock은_입력을_KST로_해석한다()
        {
            // 픽커가 준 "8/11 00:00"은 한국 시각이므로 8/10 15:00 UTC다
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
            // 픽커가 어떤 Kind로 만들어 주든 "사용자가 본 벽시계"라는 뜻은 같다 —
            // 머신 시간대에 따라 결과가 달라지면 안 된다
            var wall = new DateTime(2026, 8, 11, 0, 0, 0, kind);

            DisplayTime.FromWallClock(wall).Should().Be(new SystemTime(2026, 8, 10, 15, 0, 0));
        }

        [Fact]
        public void 폼_왕복에서_값이_변하지_않는다()
        {
            // 일정 수정: 서버 값 → 픽커 → 저장. 한 번 돌 때마다 시각이 밀리면 안 된다
            var original = new SystemTime(2026, 8, 10, 15, 0, 0);

            SystemTime roundTripped = DisplayTime.FromWallClock(original.ToWallClock());

            roundTripped.Should().Be(original);
        }

        [Fact]
        public void Format은_벽시계로_문자열을_만든다()
        {
            new SystemTime(2026, 8, 10, 15, 0, 0).Format("M/d HH:mm").Should().Be("8/11 00:00");
        }
    }
}
