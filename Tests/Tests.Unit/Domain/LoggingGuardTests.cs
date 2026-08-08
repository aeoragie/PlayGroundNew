using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace PlayGround.Tests.Unit.Domain
{
    /// <summary>
    /// 로깅 규칙을 소스에서 강제한다. 규칙은 <c>Docs/Architecture/Logging.md</c>.
    /// 로그는 빌드로도 테스트로도 안 잡히고, 운영에서 필요할 때 비어 있는 걸로만 드러난다.
    /// </summary>
    public class LoggingGuardTests
    {
        private static readonly Regex LogCall = new(
            @"\b(?:Trace|Debug|Info|Warn|Error|Fatal)With\(|\bLogWith\(|\bLog(?:Trace|Debug|Information|Warning|Error|Critical)\(",
            RegexOptions.Compiled);

        private static readonly Regex LogMessage = new(
            @"\b(?:Trace|Debug|Info|Warn|Error|Fatal)With\((?:ex(?:ception)?\w*,\s*)?""(?<text>[^""]*)""",
            RegexOptions.Compiled);

        private static readonly Regex LogField = new(@"\(""(?<key>\w+)"",", RegexOptions.Compiled);

        private static readonly Regex Hangul = new(@"[가-힣]", RegexOptions.Compiled);

        private static readonly Regex PublicResultMethod = new(
            @"public\s+async\s+Task<Result[^>]*>*\s+(?<name>\w+Async)\s*\(", RegexOptions.Compiled);

        /// <summary>식별에 쓰면 안 되는 값 — 로그는 평문으로 오래 남고 백업까지 따라간다.</summary>
        private static readonly string[] PersonalFields =
        [
            "Email", "Phone", "PhoneNumber", "BirthDate", "Birthday", "Address",
            "Password", "PasswordHash", "Token", "AccessToken", "RefreshToken",
        ];

        private static string Root() => DisplayStringPlacementTests.RepositoryRoot();

        private static IEnumerable<(string File, int Line, string Text)> SourceLines(params string[] relativePaths)
        {
            foreach (string relative in relativePaths)
            {
                string root = Path.Combine(Root(), relative.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")
                        || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                    {
                        continue;
                    }

                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        yield return (file, i + 1, lines[i]);
                    }
                }
            }
        }

        private static string Where(string file, int line) => $"{Path.GetFileName(file)}:{line}";

        [Fact]
        public void LowerLayers_DoNotLog()
        {
            // 저장소는 누가 왜 호출했는지 모른다. 실패는 Result가 메시지와 스택까지 실어 올린다.
            var offenders = SourceLines(
                    "Source/PlayGround/PlayGround.Persistence",
                    "Source/PlayGround/PlayGround.Domain",
                    "Source/PlayGround/PlayGround.Contracts")
                .Where(l => !l.Text.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .Where(l => LogCall.IsMatch(l.Text))
                .Select(l => Where(l.File, l.Line))
                .ToList();

            offenders.Should().BeEmpty(
                "비즈니스 로그는 맥락을 아는 Application에서 남긴다");
        }

        [Fact]
        public void UseCases_LogAtTheirBoundary()
        {
            // 반환 지점마다 챙기는 대신 경계에서 한 번 — 한 곳만 빠져도 그 경로의 실패는 영영 안 보인다.
            var offenders = new List<string>();
            string root = Path.Combine(Root(), "Source", "PlayGround", "PlayGround.Application");

            foreach (string file in Directory.EnumerateFiles(root, "*Command.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!PublicResultMethod.IsMatch(lines[i]))
                    {
                        continue;
                    }

                    string window = string.Join('\n', lines.Skip(i).Take(6));
                    if (!window.Contains("LogWith(mLogger", StringComparison.Ordinal))
                    {
                        offenders.Add(Where(file, i + 1));
                    }
                }
            }

            offenders.Should().BeEmpty("유즈케이스의 public 메서드는 결과를 LogWith로 남긴다");
        }

        [Fact]
        public void LogMessages_AreEnglish()
        {
            var offenders = SourceLines("Source")
                .Where(l => !l.Text.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .Select(l => (l.File, l.Line, Match: LogMessage.Match(l.Text)))
                .Where(x => x.Match.Success && Hangul.IsMatch(x.Match.Groups["text"].Value))
                .Select(x => $"{Where(x.File, x.Line)} {x.Match.Groups["text"].Value}")
                .ToList();

            offenders.Should().BeEmpty("로그 메시지는 영어다 — 수집·검색 도구가 인코딩을 가리지 않게");
        }

        [Fact]
        public void LogFields_CarryNoPersonalData()
        {
            var offenders = new List<string>();
            foreach (var (file, line, text) in SourceLines("Source"))
            {
                if (text.TrimStart().StartsWith("//", StringComparison.Ordinal) || !LogCall.IsMatch(text))
                {
                    continue;
                }

                foreach (Match field in LogField.Matches(text))
                {
                    string key = field.Groups["key"].Value;
                    if (PersonalFields.Contains(key, StringComparer.Ordinal))
                    {
                        offenders.Add($"{Where(file, line)} {key}");
                    }
                }
            }

            offenders.Should().BeEmpty("식별은 UserId·PlayerId로 한다 — 로그는 평문으로 남고 백업까지 따라간다");
        }

        [Fact]
        public void LogCalls_DoNotInterpolate()
        {
            var offenders = SourceLines("Source")
                .Where(l => !l.Text.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .Where(l => LogCall.IsMatch(l.Text) && l.Text.Contains("$\"", StringComparison.Ordinal))
                .Select(l => Where(l.File, l.Line))
                .ToList();

            offenders.Should().BeEmpty("식별자는 구조화 필드로 넘긴다 — 문자열에 묻으면 검색·집계가 안 된다");
        }
    }
}
