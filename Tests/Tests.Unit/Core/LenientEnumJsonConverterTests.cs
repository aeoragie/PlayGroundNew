using System.Text.Json;
using FluentAssertions;
using PlayGround.Contracts.Player;
using PlayGround.Contracts.Soccer;
using Xunit;

namespace PlayGround.Tests.Unit.Core
{
    /// <summary>
    /// enum 와이어 계약 — 이름 문자열로 오가고, 서버가 멤버를 추가해도 캐시된 옛 클라이언트가 죽지 않는다.
    /// </summary>
    public class LenientEnumJsonConverterTests
    {
        [Fact]
        public void Enum_SerializesAsMemberName()
        {
            var dto = new PlayerProfileDto { AgeGroup = SoccerAgeGroup.U15, Position = SoccerPosition.FW };

            string json = JsonSerializer.Serialize(dto);

            json.Should().Contain("\"AgeGroup\":\"U15\"").And.Contain("\"Position\":\"FW\"");
        }

        [Fact]
        public void Enum_RoundTrips()
        {
            var dto = new PlayerProfileDto
            {
                AgeGroup = SoccerAgeGroup.U12,
                Grade = SoccerGrade.U11,
                Position = SoccerPosition.GK,
                PreferredFoot = SoccerPreferredFoot.Both,
            };

            PlayerProfileDto restored = JsonSerializer.Deserialize<PlayerProfileDto>(JsonSerializer.Serialize(dto))!;

            restored.AgeGroup.Should().Be(SoccerAgeGroup.U12);
            restored.Grade.Should().Be(SoccerGrade.U11);
            restored.Position.Should().Be(SoccerPosition.GK);
            restored.PreferredFoot.Should().Be(SoccerPreferredFoot.Both);
        }

        [Fact]
        public void NullToken_FallsBackToUnknown()
        {
            // 비널러블 정책 — null 토큰도 미지정(Unknown)으로 받는다 (HandleNull)
            PlayerProfileDto restored = JsonSerializer.Deserialize<PlayerProfileDto>("""{"AgeGroup":null}""")!;

            restored.AgeGroup.Should().Be(SoccerAgeGroup.Unknown);
        }

        [Fact]
        public void UnknownName_FallsBackToUnknown_InsteadOfThrowing()
        {
            // 서버가 나중에 추가한 멤버(예: U10)를 옛 클라이언트가 받는 상황 — 역직렬화가 죽으면 화면 전체가 죽는다
            PlayerProfileDto restored = JsonSerializer.Deserialize<PlayerProfileDto>("""{"AgeGroup":"U10"}""")!;

            restored.AgeGroup.Should().Be(SoccerAgeGroup.Unknown);
        }

        [Fact]
        public void NumericToken_FallsBackToUnknown()
        {
            // 이름 문자열만이 와이어 계약이다 — 정수 직렬화 클라이언트가 생기지 않게 숫자는 받지 않는다
            PlayerProfileDto restored = JsonSerializer.Deserialize<PlayerProfileDto>("""{"AgeGroup":2}""")!;

            restored.AgeGroup.Should().Be(SoccerAgeGroup.Unknown);
        }
    }
}
