using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace PlayGround.Tests.Unit.Domain
{
    /// <summary>
    /// **표시 문자열은 Domain·Contracts에 두지 않는다** — 표현 계층(Client의 AppText)이 소유한다.
    /// Domain은 Client를 참조할 수 없어 리소스에 닿지 못하므로, 여기에 라벨이 생기면
    /// 그 문구만 영원히 번역되지 않는다(실제로 SoccerCareerOutcomeType·SoccerRecordCorrection에서 겪었다).
    /// 근거·예외는 Docs/Architecture/Localization.md §7.
    /// </summary>
    public class DisplayStringPlacementTests
    {
        private static readonly Regex Hangul = new(@"[가-힣]", RegexOptions.Compiled);

        /// <summary>주석(`///`, `//`)과 문자열 밖의 한글은 대상이 아니다 — 문자열 리터럴만 본다.</summary>
        private static readonly Regex StringLiteral = new("\"(?:[^\"\\\\]|\\\\.)*\"", RegexOptions.Compiled);

        public static TheoryData<string> Projects =>
            new() { "PlayGround.Domain", "PlayGround.Contracts" };

        [Theory]
        [MemberData(nameof(Projects))]
        public void 표시_문자열이_없다(string project)
        {
            string root = Path.Combine(RepositoryRoot(), "Source", "PlayGround", project);
            Directory.Exists(root).Should().BeTrue($"{project} 경로를 찾지 못했다");

            var offenders = new List<string>();
            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = StripComment(lines[i]);
                    foreach (Match literal in StringLiteral.Matches(line))
                    {
                        if (Hangul.IsMatch(literal.Value))
                        {
                            offenders.Add($"{Path.GetFileName(file)}:{i + 1} {literal.Value}");
                        }
                    }
                }
            }

            offenders.Should().BeEmpty(
                "표시 라벨은 Client의 리소스로 옮긴다 (Models/SoccerDomainEnumLabels.cs 참조)");
        }

        /// <summary>`//` 뒤를 잘라낸다. 문자열 안의 `//`(URL 등)는 한글이 없어 오탐이 생기지 않는다.</summary>
        private static string StripComment(string line)
        {
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            return comment < 0 ? line : line[..comment];
        }

        internal static string RepositoryRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "PlayGround.slnx")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("리포지토리 루트를 찾지 못했다");
        }
    }
}
