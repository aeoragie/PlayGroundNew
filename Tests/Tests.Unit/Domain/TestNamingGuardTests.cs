using FluentAssertions;
using System.Text.RegularExpressions;
using Xunit;

namespace PlayGround.Tests.Unit.Domain
{
    /// <summary>
    /// 테스트 메서드·타입 이름은 ASCII(<c>Subject_Behavior</c>)로 쓴다 — 한글 식별자는
    /// 테스트 로그·TRX·<c>--filter-method</c>에서 깨진다. 근거는 Testing.md §5-6.
    /// </summary>
    public class TestNamingGuardTests
    {
        private static readonly Regex TestMethod = new(
            @"\bpublic\s+(?:async\s+)?(?:Task|void|ValueTask)\s+(?<name>[^\s(]+)\s*\(",
            RegexOptions.Compiled);

        private static readonly Regex TypeDeclaration = new(
            @"\b(?:class|record|struct|enum)\s+(?<name>[^\s:<{(]+)",
            RegexOptions.Compiled);

        // 이스케이프로 쓴다 — 제어문자를 그대로 넣으면 파일에 NUL이 박혀 git이 바이너리로 본다
        private static readonly Regex NonAscii = new(@"[^\x00-\x7F]", RegexOptions.Compiled);

        public static TheoryData<string> TestProjects =>
            new() { "Tests.Unit", "Tests.Integration", "Tests.Infrastructure" };

        [Theory]
        [MemberData(nameof(TestProjects))]
        public void TestIdentifiers_AreAscii(string project)
        {
            string root = Path.Combine(DisplayStringPlacementTests.RepositoryRoot(), "Tests", project);
            Directory.Exists(root).Should().BeTrue($"{project} 경로를 찾지 못했다");

            var offenders = new List<string>();
            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    // 주석·문자열은 걷어낸다 — 설명과 실패 메시지의 한글은 권장이지 위반이 아니다
                    string line = StripNoise(lines[i]);

                    foreach (Regex pattern in new[] { TestMethod, TypeDeclaration })
                    {
                        Match match = pattern.Match(line);
                        if (match.Success && NonAscii.IsMatch(match.Groups["name"].Value))
                        {
                            offenders.Add($"{Path.GetFileName(file)}:{i + 1} {match.Groups["name"].Value}");
                        }
                    }
                }
            }

            offenders.Should().BeEmpty(
                "테스트 이름은 ASCII로 쓴다(Subject_Behavior). 설명은 주석과 실패 메시지에 남긴다");
        }

        /// <summary>주석과 문자열 리터럴을 걷어낸다.</summary>
        private static string StripNoise(string line)
        {
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            if (comment >= 0)
            {
                line = line[..comment];
            }

            return Regex.Replace(line, @"""(?:[^""\\]|\\.)*""", "\"\"");
        }
    }
}
