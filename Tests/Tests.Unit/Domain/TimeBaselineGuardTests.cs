using System.Text.RegularExpressions;
using FluentAssertions;
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
    /// - C#: 순간은 `SystemTime` **타입**으로만 다룬다 — `DateTime`은 타입 자체를 쓰지 않는다.
    ///   달력 날짜(생년월일·커리어 기간)는 `DateOnly`.
    /// - **시간대를 아는 곳은 `DisplayTime` 하나다**(보는 사람 기준). 저장·전송은 전부 UTC 순간이라
    ///   서버·DB는 시간대를 모른다. 그 밖의 `ToLocalTime()`·`TimeZoneInfo.Local`은 전부 금지다 —
    ///   시간대 지식이 흩어지면 어디를 고쳐야 하는지 알 수 없게 된다.
    /// - SQL: `GETUTCDATE()`만. `GETDATE()`·`SYSDATETIME()`은 서버 시간대에 묶인다
    /// </summary>
    public class TimeBaselineGuardTests
    {
        /// <summary>`DateTime.Now` 같은 직접 호출과 호스트 시간대 의존(`ToLocalTime`·`TimeZoneInfo.Local`).
        /// 표시 변환은 Client의 `DisplayTime`만 안다 — 주석·문자열은 미리 걷어낸 뒤 찾는다.</summary>
        private static readonly Regex DirectClock = new(
            @"\bDateTime(?:Offset)?\s*\.\s*(?:Now|UtcNow|Today)\b|\.\s*ToLocalTime\s*\(|\bTimeZoneInfo\s*\.\s*Local\b",
            RegexOptions.Compiled);

        /// <summary>`DateTime` 타입 사용 자체 — `UtcDateTime`·`DateTimeKind` 같은 합성어는 걸리지 않는다.</summary>
        private static readonly Regex DateTimeType = new(
            @"\bDateTime\b(?!\s*Offset)", RegexOptions.Compiled);

        /// <summary>서버 시간대에 묶이는 SQL 내장 함수 — UTC 서버에서 조용히 어긋난다.</summary>
        private static readonly Regex LocalSqlClock = new(
            @"\b(?:GETDATE|SYSDATETIME|SYSDATETIMEOFFSET|CURRENT_TIMESTAMP)\s*\(?", RegexOptions.Compiled);

        /// <summary>시계 직접 호출 예외 — 래퍼 자신뿐이다.</summary>
        private static readonly string[] AllowedFiles =
        {
            "SystemTime.cs",        // 시계 읽기의 원천
            "DisplayTime.cs",       // 보는 사람의 시간대
        };

        /// <summary>`DateTime` 타입 허용 파일 — 전부 "원시 시각과 만나는 경계"다. 늘리기 전에 재고한다.</summary>
        private static readonly string[] AllowedTypeFiles =
        {
            "SystemTime.cs",            // 래퍼 자신
            "SystemTimeTypeHandler.cs", // Dapper ↔ DB 경계
        };

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
                "시각은 SystemTime.Now(UTC)로 얻는다. 시간대를 아는 곳은 DisplayTime 하나뿐이다");
        }

        /// <summary>Client는 제외한다 — 표시층은 브라우저 로컬 벽시계(DateTime)를 다루는 것이 설계다.</summary>
        public static TheoryData<string> NonDisplayRoots =>
            new()
            {
                Path.Combine("Source", "Core"),
                Path.Combine("Source", "PlayGround", "PlayGround.Contracts"),
                Path.Combine("Source", "PlayGround", "PlayGround.Domain"),
                Path.Combine("Source", "PlayGround", "PlayGround.Application"),
                Path.Combine("Source", "PlayGround", "PlayGround.Persistence"),
                Path.Combine("Source", "PlayGround", "PlayGround.Server"),
            };

        [Theory]
        [MemberData(nameof(NonDisplayRoots))]
        public void DateTime_타입을_쓰지_않는다(string relativeRoot)
        {
            string root = Path.Combine(DisplayStringPlacementTests.RepositoryRoot(), relativeRoot);
            Directory.Exists(root).Should().BeTrue($"{relativeRoot} 경로를 찾지 못했다");

            var offenders = new List<string>();
            foreach (string file in EnumerateSource(root))
            {
                if (AllowedTypeFiles.Contains(Path.GetFileName(file)))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (DateTimeType.IsMatch(StripNoise(lines[i])))
                    {
                        offenders.Add($"{Path.GetFileName(file)}:{i + 1} {lines[i].Trim()}");
                    }
                }
            }

            offenders.Should().BeEmpty(
                "순간은 SystemTime, 달력 날짜는 DateOnly로 다룬다 — 원시 DateTime은 경계 파일(AllowedTypeFiles)에만 존재한다");
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
            SystemTime.Now.UtcDateTime.Kind.Should().Be(DateTimeKind.Utc);
            SystemTime.Now.UtcDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void 직렬화는_ISO8601_Z다()
        {
            // 기존 DateTime(UTC) 직렬화와 와이어 포맷이 같아야 한다 — Z가 빠지면
            // 브라우저가 변환 기준을 잃어 표시가 조용히 어긋난다
            var value = new SystemTime(2026, 8, 10, 12, 30, 0);
            string json = System.Text.Json.JsonSerializer.Serialize(value);

            json.Should().Be("\"2026-08-10T12:30:00Z\"");
            System.Text.Json.JsonSerializer.Deserialize<SystemTime>(json).Should().Be(value);
        }

        [Fact]
        public void 어떤_Kind로_만들어도_UTC로_정규화된다()
        {
            // DB에서 읽은 값(Unspecified)은 UTC로 표식하고, 로컬 값은 UTC로 변환한다
            var unspecified = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Unspecified);
            new SystemTime(unspecified).UtcDateTime.Kind.Should().Be(DateTimeKind.Utc);
            new SystemTime(unspecified).UtcDateTime.Hour.Should().Be(12);

            var local = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Local);
            new SystemTime(local).UtcDateTime.Should().Be(local.ToUniversalTime());
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
