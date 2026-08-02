using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace PlayGround.Tests.Unit.Domain
{
    /// <summary>
    /// **모든 `.sql`이 sqlproj의 검증 대상에 들어 있어야 한다.**
    /// 항목을 손으로 나열하던 시절 Soccer에서 56개(테이블 13·프로시저 37 등)가 누락됐고,
    /// 빠진 테이블을 참조하는 프로시저가 전부 SQL71502(확인되지 않은 참조)로 새고 있었다.
    /// 지금은 폴더 글롭이라 파일 추가는 자동으로 잡히지만, **새 폴더**를 만들고 글롭을 안 넣으면
    /// 그 폴더가 통째로 검증에서 빠진다 — 이 테스트가 그 경우를 막는다.
    /// </summary>
    public class SqlProjectCoverageTests
    {
        /// <summary>`<Build Include="Tables\**\*.sql" />` 의 폴더 부분만 뽑는다.</summary>
        private static readonly Regex GlobFolder =
            new(@"<(?:Build|None) Include=""([^""\\]+)\\\*\*\\\*\.sql""\s*/>", RegexOptions.Compiled);

        public static TheoryData<string> Projects =>
            new() { "Soccer", "Account" };

        [Theory]
        [MemberData(nameof(Projects))]
        public void 모든_sql_폴더가_프로젝트_글롭에_덮인다(string database)
        {
            string root = Path.Combine(DisplayStringPlacementTests.RepositoryRoot(), "Source", "Database", database);
            string projectFile = Path.Combine(root, $"Database.{database}.sqlproj");
            File.Exists(projectFile).Should().BeTrue();

            var covered = GlobFolder.Matches(File.ReadAllText(projectFile))
                .Select(m => m.Groups[1].Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // .sql이 실제로 들어 있는 폴더만 본다 (빈 폴더는 글롭이 없어도 무해)
            List<string> uncovered = Directory
                .EnumerateFiles(root, "*.sql", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(root, f).Split(Path.DirectorySeparatorChar)[0])
                .Distinct()
                .Where(folder => !covered.Contains(folder))
                .ToList();

            uncovered.Should().BeEmpty(
                $"Database.{database}.sqlproj 에 <Build> 또는 <None> 글롭을 추가해야 한다");
        }

        [Theory]
        [MemberData(nameof(Projects))]
        public void 개별_파일_나열을_쓰지_않는다(string database)
        {
            // 손으로 관리하면 반드시 드리프트한다 — 글롭만 쓴다.
            string projectFile = Path.Combine(DisplayStringPlacementTests.RepositoryRoot(),
                "Source", "Database", database, $"Database.{database}.sqlproj");

            var explicitEntries = Regex
                .Matches(File.ReadAllText(projectFile), @"<(?:Build|None) Include=""([^""]+\.sql)""")
                .Select(m => m.Groups[1].Value)
                .Where(include => !include.Contains('*'))   // 글롭 자체는 대상이 아니다
                .ToList();

            explicitEntries.Should().BeEmpty("폴더 글롭(`Folder\\**\\*.sql`)으로 대체한다");
        }

        [Fact]
        public void 스키마_폴더와_데이터_폴더를_구분해_넣는다()
        {
            // Seeds(데이터)·Queries(제너레이터용 SELECT)·Migrations(적용 이력)는 CREATE 구문이 아니라
            // 빌드에 넣으면 모델이 깨진다. Build/None을 뒤바꾸지 않았는지 본다.
            string projectFile = Path.Combine(DisplayStringPlacementTests.RepositoryRoot(),
                "Source", "Database", "Soccer", "Database.Soccer.sqlproj");
            string content = File.ReadAllText(projectFile);

            foreach (string schema in new[] { "Tables", "Procedures", "Functions", "Indexes", "Schema" })
            {
                content.Should().Contain($@"<Build Include=""{schema}\**\*.sql"" />");
            }

            foreach (string data in new[] { "Seeds", "Queries", "Migrations" })
            {
                content.Should().Contain($@"<None Include=""{data}\**\*.sql"" />");
            }
        }
    }
}
