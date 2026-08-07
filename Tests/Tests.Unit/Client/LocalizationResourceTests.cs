using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;
using PlayGround.Client.Localization;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>
    /// i18n 리소스 정합성. **생성기(Generator.Localization)는 수동 실행**이라 JSON만 고치고
    /// 재생성을 잊으면 화면에 키 문자열이 그대로 뜬다 — 빌드는 통과하므로 여기서 잡는다.
    /// ja 누락·플레이스홀더 인덱스 불일치는 문화권 전환 시 문장이 깨지거나 예외로 이어진다.
    /// </summary>
    [Collection(LocalizationCollection.Name)]
    public class LocalizationResourceTests(LocalizationFixture fixture)
    {
        /// <summary>`{0}` / `{0:이/가}` 둘 다 잡는다 — 조사 모디파이어도 같은 인자를 가리킨다.</summary>
        private static readonly Regex Placeholder = new(@"\{(\d+)(?::[^}]*)?\}", RegexOptions.Compiled);

        private static readonly Regex ParticleModifier = new(@"\{\d+:[^}]*/[^}]*\}", RegexOptions.Compiled);

        private static string PlaceholderSet(string value) =>
            string.Join(',', Placeholder.Matches(value).Select(m => int.Parse(m.Groups[1].Value)).Distinct().Order());

        public static TheoryData<string> Domains
        {
            get
            {
                var data = new TheoryData<string>();
                foreach (string domain in AppText.Domains)
                {
                    data.Add(domain);
                }

                return data;
            }
        }

        //.// 파일 존재·구조

        [Theory]
        [MemberData(nameof(Domains))]
        public void EveryDomain_HasKoAndJaFiles(string domain)
        {
            File.Exists(Path.Combine(fixture.ResourceDirectory, $"{domain}.ko.json")).Should().BeTrue();
            File.Exists(Path.Combine(fixture.ResourceDirectory, $"{domain}.ja.json")).Should().BeTrue();
        }

        [Fact]
        public void ResourceFiles_MatchGeneratorDomainList()
        {
            // JSON을 새로 넣고 생성기를 안 돌리면 그 도메인은 런타임에 로드되지 않는다
            string[] onDisk = Directory.GetFiles(fixture.ResourceDirectory, "*.ko.json")
                .Select(f => Path.GetFileName(f).Split('.')[0])
                .Order()
                .ToArray();

            onDisk.Should().Equal(AppText.Domains.Order().ToArray());
        }

        //.// ko ↔ ja 정합

        [Theory]
        [MemberData(nameof(Domains))]
        public void Ja_HasEveryKoKey(string domain)
        {
            fixture.Load(domain, "ko").Keys
                .Except(fixture.Load(domain, "ja").Keys)
                .Should().BeEmpty($"{domain}.ja.json 에 번역이 빠졌다");
        }

        [Theory]
        [MemberData(nameof(Domains))]
        public void Ja_HasNoKeyMissingFromKo(string domain)
        {
            // ko가 스키마의 기준 — ja에만 있는 키는 생성되지 않아 영영 쓰이지 않는다
            fixture.Load(domain, "ja").Keys
                .Except(fixture.Load(domain, "ko").Keys)
                .Should().BeEmpty($"{domain}.ko.json 에 먼저 키를 넣어야 한다");
        }

        [Theory]
        [MemberData(nameof(Domains))]
        public void KoAndJa_HaveSamePlaceholderIndexes(string domain)
        {
            // ja에서 {0}이 빠지면 인자가 무시되고, 인덱스가 더 크면 FormatException이 난다
            Dictionary<string, string> ja = fixture.Load(domain, "ja");

            foreach ((string key, string koValue) in fixture.Load(domain, "ko"))
            {
                if (!ja.TryGetValue(key, out string? jaValue))
                {
                    continue; // 누락은 위 테스트가 잡는다
                }

                PlaceholderSet(jaValue).Should().Be(PlaceholderSet(koValue), $"{domain}.{key}");
            }
        }

        [Theory]
        [MemberData(nameof(Domains))]
        public void Ja_HasNoKoreanParticleModifier(string domain)
        {
            // 조사는 한국어 문법 — ja 값에 남아 있으면 "{0}이/가" 가 그대로 화면에 뜬다
            foreach ((string key, string value) in fixture.Load(domain, "ja"))
            {
                ParticleModifier.IsMatch(value).Should().BeFalse($"{domain}.{key} = {value}");
            }
        }

        [Theory]
        [MemberData(nameof(Domains))]
        public void KoValues_AreNotEmpty(string domain)
        {
            foreach ((string key, string value) in fixture.Load(domain, "ko"))
            {
                value.Should().NotBeNullOrWhiteSpace($"{domain}.{key}");
            }
        }

        //.// 생성물 최신성 — 이 스위트의 핵심

        [Fact]
        public void GeneratedAccessors_ResolveEveryKey()
        {
            // 생성기를 안 돌린 채 JSON에서 키를 지우거나 이름을 바꾸면 여기서 걸린다.
            // (반대로 JSON에만 키를 추가한 경우는 접근자가 없어 컴파일 단계에서 걸린다.)
            List<string> unresolved = InvokeAllAccessors()
                .Where(a => a.Value is string text && text == a.Key)
                .Select(a => a.Key)
                .ToList();

            unresolved.Should().BeEmpty(
                "생성물이 리소스와 어긋났다 — cd Source/Tools/Generator.Localization && dotnet run");
        }

        [Fact]
        public void AccessorsWithArguments_SubstitutePlaceholders()
        {
            // 치환에 실패하면 "{0}" 이 그대로 남는다 (인자 개수 불일치·FormatException 폴백)
            List<string> leftover = InvokeAllAccessors()
                .Where(a => a.Value is string text && Placeholder.IsMatch(text))
                .Select(a => $"{a.Key} → {a.Value}")
                .ToList();

            leftover.Should().BeEmpty();
        }

        [Fact]
        public void ResolvedText_LeavesNoParticleModifier()
        {
            // KoreanParticle이 못 푼 모디파이어는 "{0:이/가}" 그대로 화면에 뜬다
            List<string> leftover = InvokeAllAccessors()
                .Where(a => a.Value is string text && ParticleModifier.IsMatch(text))
                .Select(a => $"{a.Key} → {a.Value}")
                .ToList();

            leftover.Should().BeEmpty();
        }

        /// <summary>AppText의 도메인별 중첩 클래스를 훑어 프로퍼티·메서드를 전부 호출한다.</summary>
        private static IEnumerable<(string Key, object? Value)> InvokeAllAccessors()
        {
            foreach (Type domainType in typeof(AppText).GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
            {
                foreach (PropertyInfo property in domainType.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    yield return ($"{domainType.Name}.{property.Name}", property.GetValue(null));
                }

                foreach (MethodInfo method in domainType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.IsSpecialName)
                    {
                        continue; // 프로퍼티 getter는 위에서 처리했다
                    }

                    // 생성된 인자는 전부 object — 받침 판정까지 타도록 한글 값을 넣는다
                    object[] args = method.GetParameters().Select(_ => (object)"강동").ToArray();
                    yield return ($"{domainType.Name}.{method.Name}", method.Invoke(null, args));
                }
            }
        }
    }
}
