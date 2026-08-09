using FluentAssertions;
using PlayGround.Client.Localization;
using Xunit;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>리소스 조사 모디파이어 `{n:받침있음/받침없음}` 해석 규칙.
    /// 컴포넌트에 문화권 분기를 두지 않기 위한 핵심 로직이라 회귀가 나면 문장이 통째로 어색해진다.</summary>
    public class KoreanParticleTests
    {
        //.// 한글 받침

        [Theory]
        [InlineData("검증fc", "{0:이/가} 왔어요", "{0}가 왔어요")]      // c → 영문 받침 없음
        [InlineData("강동", "{0:이/가} 왔어요", "{0}이 왔어요")]        // 동 → 종성 ㅇ
        [InlineData("서초", "{0:이/가} 왔어요", "{0}가 왔어요")]        // 초 → 종성 없음
        [InlineData("강동", "{0:은/는} 팀", "{0}은 팀")]
        [InlineData("서초", "{0:은/는} 팀", "{0}는 팀")]
        [InlineData("강동", "{0:을/를} 초대", "{0}을 초대")]
        [InlineData("서초", "{0:을/를} 초대", "{0}를 초대")]
        public void Resolve_PicksParticleByFinalConsonant(string value, string template, string expected)
        {
            KoreanParticle.Resolve(template, [value]).Should().Be(expected);
        }

        //.// 으로/로 — ㄹ 받침 예외

        [Theory]
        [InlineData("이메일", "{0}로")]        // ㄹ 받침 → '로' (이메일으로 아님)
        [InlineData("구글", "{0}로")]          // ㄹ 받침
        [InlineData("카카오", "{0}로")]        // 받침 없음
        [InlineData("네이버", "{0}로")]        // 받침 없음
        [InlineData("애플", "{0}로")]          // ㄹ 받침
        [InlineData("강동", "{0}으로")]        // ㅇ 받침 → '으로'
        public void Resolve_TreatsRieulAsVowel_ForEuroRo(string value, string expected)
        {
            KoreanParticle.Resolve("{0:으로/로}", [value]).Should().Be(expected);
        }

        [Fact]
        public void Resolve_AppliesSameRule_ToSuffixForms()
        {
            KoreanParticle.Resolve("{0:으로도/로도} 로그인", ["구글"]).Should().Be("{0}로도 로그인");
            KoreanParticle.Resolve("{0:으로도/로도} 로그인", ["강동"]).Should().Be("{0}으로도 로그인");
        }

        //.// 숫자·영문 발음

        [Theory]
        [InlineData("3", "{0}이")]   // 삼 → 받침
        [InlineData("6", "{0}이")]   // 육 → 받침
        [InlineData("2", "{0}가")]   // 이 → 받침 없음
        [InlineData("5", "{0}가")]   // 오 → 받침 없음
        [InlineData("FC", "{0}가")]  // 씨 → 받침 없음
        [InlineData("PL", "{0}이")]  // 엘 → 받침
        public void Resolve_JudgesDigitsAndLatin_ByKoreanPronunciation(string value, string expected)
        {
            KoreanParticle.Resolve("{0:이/가}", [value]).Should().Be(expected);
        }

        //.// 방어

        [Fact]
        public void Resolve_ReturnsOriginal_WhenNoModifier()
        {
            KoreanParticle.Resolve("{0} 선수를 추가했어요.", ["강동"]).Should().Be("{0} 선수를 추가했어요.");
        }

        [Fact]
        public void Resolve_KeepsOriginal_WhenArgumentsMissing()
        {
            KoreanParticle.Resolve("{0:이/가} {1:을/를}", ["강동"]).Should().Be("{0}이 {1:을/를}");
        }

        [Fact]
        public void Resolve_TreatsEmptyAsVowelEnding()
        {
            KoreanParticle.Resolve("{0:이/가}", [string.Empty]).Should().Be("{0}가");
        }

        [Fact]
        public void Resolve_ResolvesEachModifierIndependently()
        {
            KoreanParticle.Resolve("{0:이/가} {1:을/를} 초대했어요", ["강동", "서초"])
                .Should().Be("{0}이 {1}를 초대했어요");
        }
    }
}
