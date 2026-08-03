using System.Text.RegularExpressions;
using FluentAssertions;
using PlayGround.Domain.Time;
using PlayGround.Shared.Time;
using Xunit;

namespace PlayGround.Tests.Unit.Domain
{
    /// <summary>
    /// **시각 기준은 UTC 하나다** (ReleasePlan H7).
    ///
    /// 이 가드가 없으면 새 코드가 `DateTime.Now`를 다시 쓰기 시작한다 — 개발 PC(KST)에서는
    /// 멀쩡히 돌고 **UTC 서버에서만 9시간 어긋나서**, 리뷰에서도 테스트에서도 안 잡힌다.
    /// 실제로 그 상태로 7곳이 쌓여 있었다.
    ///
    /// 규칙:
    /// - C#: `SystemTime.Now`(UTC) — 한국 달력이 기준인 값만 `KoreanTime`
    /// - SQL: `GETUTCDATE()`만. `GETDATE()`·`SYSDATETIME()`은 서버 시간대에 묶인다
    /// </summary>
    public class TimeBaselineGuardTests
    {
        /// <summary>`DateTime.Now` 같은 직접 호출. 주석·문자열은 미리 걷어낸 뒤 찾는다.</summary>
        private static readonly Regex DirectClock = new(
            @"\bDateTime(?:Offset)?\s*\.\s*(?:Now|UtcNow|Today)\b", RegexOptions.Compiled);

        /// <summary>서버 시간대에 묶이는 SQL 내장 함수 — UTC 서버에서 조용히 어긋난다.</summary>
        private static readonly Regex LocalSqlClock = new(
            @"\b(?:GETDATE|SYSDATETIME|SYSDATETIMEOFFSET|CURRENT_TIMESTAMP)\s*\(?", RegexOptions.Compiled);

        /// <summary>래퍼 자신은 당연히 예외다. 그 외에는 없어야 한다.</summary>
        private static readonly string[] AllowedFiles = { "SystemTime.cs" };

        public static TheoryData<string> SourceRoots =>
            new()
            {
                Path.Combine("Source", "Core"),
                Path.Combine("Source", "PlayGround"),
            };

        [Theory]
        [MemberData(nameof(SourceRoots))]
        public void DateTime을_직접_호출하지_않는다(string relativeRoot)
        {
            string root = Path.Combine(DisplayStringPlacementTests.RepositoryRoot(), relativeRoot);
            Directory.Exists(root).Should().BeTrue($"{relativeRoot} 경로를 찾지 못했다");

            var offenders = new List<string>();
            foreach (string file in EnumerateSource(root))
            {
                if (AllowedFiles.Contains(Path.GetFileName(file)))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (DirectClock.IsMatch(StripNoise(lines[i])))
                    {
                        offenders.Add($"{Path.GetFileName(file)}:{i + 1} {lines[i].Trim()}");
                    }
                }
            }

            offenders.Should().BeEmpty(
                "시각은 SystemTime.Now(UTC)로 얻는다. 한국 달력이 기준인 값(마감일·시즌 연도)만 KoreanTime을 쓴다");
        }

        [Fact]
        public void SQL은_GETUTCDATE만_쓴다()
        {
            string root = Path.Combine(DisplayStringPlacementTests.RepositoryRoot(), "Source", "Database");
            Directory.Exists(root).Should().BeTrue();

            var offenders = new List<string>();
            foreach (string file in Directory.EnumerateFiles(root, "*.sql", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    // GETUTCDATE는 이 정규식에 걸리지 않는다(이름이 다르다) — 지역 시각 함수만 잡는다
                    if (LocalSqlClock.IsMatch(StripSqlComment(lines[i])))
                    {
                        offenders.Add($"{Path.GetFileName(file)}:{i + 1} {lines[i].Trim()}");
                    }
                }
            }

            offenders.Should().BeEmpty(
                "GETDATE()·SYSDATETIME()은 서버 시간대에 묶인다. 저장·비교는 GETUTCDATE()로만 한다");
        }

        [Fact]
        public void SystemTime은_UTC를_돌려준다()
        {
            // 이름이 Now라서 지역 시각으로 오해하기 쉽다 — 계약을 못 박아 둔다
            SystemTime.Now.Kind.Should().Be(DateTimeKind.Utc);
            SystemTime.Now.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void KoreanTime은_UTC보다_9시간_앞선다()
        {
            (KoreanTime.Now - SystemTime.Now).Should().BeCloseTo(TimeSpan.FromHours(9), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void 마감일은_한국_하루의_끝을_UTC로_저장한다()
        {
            // "8/10 마감"은 8/10 23:59:59.999... KST까지 유효하다 = 8/10 14:59:59.999... UTC
            DateTime utc = KoreanTime.EndOfDayToUtc(new DateTime(2026, 8, 10));

            utc.Kind.Should().Be(DateTimeKind.Utc);
            utc.Should().BeCloseTo(new DateTime(2026, 8, 10, 14, 59, 59, 999, DateTimeKind.Utc), TimeSpan.FromSeconds(1));

            // 되돌리면 원래 한국 날짜여야 한다
            KoreanTime.ToKoreanDate(utc).Should().Be(new DateTime(2026, 8, 10));
        }

        [Fact]
        public void 마감_경계가_한국_자정에서_갈린다()
        {
            DateTime deadline = KoreanTime.EndOfDayToUtc(new DateTime(2026, 8, 10));

            // 8/10 23:59 KST = 8/10 14:59 UTC → 아직 열려 있다
            DateTime justBefore = KoreanTime.ToUtc(new DateTime(2026, 8, 10, 23, 59, 0));
            // 8/11 00:01 KST = 8/10 15:01 UTC → 닫혔다. UTC 날짜로 비교하면 여기서 틀린다
            DateTime justAfter = KoreanTime.ToUtc(new DateTime(2026, 8, 11, 0, 1, 0));

            (deadline > justBefore).Should().BeTrue("한국 시각 8/10 23:59는 마감 전이다");
            (deadline > justAfter).Should().BeFalse("한국 시각 8/11 00:01은 마감 후다");
        }

        [Fact]
        public void 시즌_범위는_한국_달력_한_해를_덮는다()
        {
            (DateTime startUtc, DateTime endUtc) = KoreanTime.YearRangeUtc(2026);

            // 2026-01-01 00:00 KST = 2025-12-31 15:00 UTC
            startUtc.Should().Be(new DateTime(2025, 12, 31, 15, 0, 0, DateTimeKind.Utc));
            endUtc.Should().Be(new DateTime(2026, 12, 31, 15, 0, 0, DateTimeKind.Utc));

            // 1/1 오전 8시(KST) 경기 — UTC로는 전해 12/31이라 YEAR(UTC) 비교였다면 빠졌을 값
            DateTime earlyMorningMatch = KoreanTime.ToUtc(new DateTime(2026, 1, 1, 8, 0, 0));
            (earlyMorningMatch >= startUtc && earlyMorningMatch < endUtc).Should().BeTrue();
        }

        //.// 헬퍼

        private static IEnumerable<string> EnumerateSource(string root) =>
            Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                            || f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")
                            && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                            && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        /// <summary>주석과 문자열 리터럴을 걷어낸다 — 설명문의 `DateTime.Now`까지 잡으면 못 쓴다.</summary>
        private static string StripNoise(string line)
        {
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            if (comment >= 0)
            {
                line = line[..comment];
            }

            line = Regex.Replace(line, @"""(?:[^""\\]|\\.)*""", "\"\"");
            return line;
        }

        private static string StripSqlComment(string line)
        {
            int comment = line.IndexOf("--", StringComparison.Ordinal);
            return comment >= 0 ? line[..comment] : line;
        }
    }
}
