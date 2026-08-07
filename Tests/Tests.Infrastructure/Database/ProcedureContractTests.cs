using System.Reflection;
using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;

using Xunit;

namespace PlayGround.Tests.Infrastructure.Database
{
    /// <summary>
    /// **생성된 프로시저 호출 객체 ↔ 실제 DB 프로시저**의 계약.
    ///
    /// 이 어긋남은 빌드로는 절대 안 잡히고 런타임에서야 터진다. 실제로 겪은 것들:
    /// SQL만 고치고 제너레이터를 안 돌림 · DB에 프로시저를 배포 안 함(CLAUDE.md "DB 동기화") ·
    /// 프로시저가 sqlproj 빌드에서 빠져 있어 아무도 검증하지 않음(2026-08-02 `9963b21`).
    ///
    /// 생성 객체 전량을 리플렉션으로 훑으므로 **프로시저를 새로 만들어도 자동으로 대상이 된다.**
    /// </summary>
    [Collection(LocalDatabaseCollection.Name)]
    public class ProcedureContractTests(LocalDatabaseFixture fixture)
    {
        /// <summary>Dapper가 프로시저 RETURN 값을 담는 자리 — SQL 파라미터가 아니다.</summary>
        private const string ReturnValueParameter = "ReturnValue";

        /// <summary>DB에 실제로 있는 프로시저 이름 + 파라미터 (프로시저당 1회 조회).</summary>
        private static readonly Dictionary<DatabaseTypes, Lazy<Dictionary<string, HashSet<string>>>> Actual = new()
        {
            [DatabaseTypes.Account] = new(() => LoadParameters(DatabaseTypes.Account)),
            [DatabaseTypes.Soccer] = new(() => LoadParameters(DatabaseTypes.Soccer)),
        };

        public static TheoryData<string> GeneratedProcedures
        {
            get
            {
                var data = new TheoryData<string>();
                foreach (Type type in FindGeneratedProcedureTypes())
                {
                    data.Add(type.FullName!);
                }

                return data;
            }
        }

        [Fact]
        public void GeneratedProcedureTypes_AreDiscovered()
        {
            // 리플렉션 탐색이 조용히 0건이 되면 아래 테스트가 전부 통과처럼 보인다
            FindGeneratedProcedureTypes().Should().NotBeEmpty();
        }

        [Theory]
        [MemberData(nameof(GeneratedProcedures))]
        public void Procedures_AreDeployedToDatabase(string typeName)
        {
            fixture.SkipIfUnavailable();

            Type type = ResolveType(typeName);
            (DatabaseTypes database, string procedure, _) = Describe(type);

            Actual[database].Value.Keys.Should().Contain(procedure,
                $"{database} DB에 {procedure} 가 없다 — Source/Database/{database}/Procedures 를 배포한다");
        }

        [Theory]
        [MemberData(nameof(GeneratedProcedures))]
        public void ParameterNames_MatchDatabase(string typeName)
        {
            fixture.SkipIfUnavailable();

            Type type = ResolveType(typeName);
            (DatabaseTypes database, string procedure, HashSet<string> generated) = Describe(type);

            if (!Actual[database].Value.TryGetValue(procedure, out HashSet<string>? actual))
            {
                Assert.Skip($"{procedure} 미배포 — 존재 테스트가 이미 보고한다");
                return;
            }

            // C#이 보내는데 DB가 모르는 파라미터 → 실행 시 "지정한 인수가 너무 많습니다"
            generated.Except(actual).Should().BeEmpty(
                $"{procedure}: 생성 객체가 보내는 파라미터를 DB가 모른다 (제너레이터 재실행 또는 프로시저 재배포)");

            // DB가 요구하는데 C#이 안 보내는 파라미터 → 기본값이 없으면 실행 실패
            actual.Except(generated).Should().BeEmpty(
                $"{procedure}: DB 파라미터가 생성 객체에 없다 (SQL 수정 후 제너레이터 미실행)");
        }

        //.// 리플렉션 — 생성된 ProcedureBase 파생 전량

        private static IEnumerable<Type> FindGeneratedProcedureTypes()
        {
            return typeof(RepositoryBase).Assembly.GetName().Name is null
                ? []
                : PersistenceAssembly().GetTypes()
                    .Where(t => !t.IsAbstract && typeof(ProcedureBase).IsAssignableFrom(t))
                    .Where(t => t.Namespace?.Contains(".Generated.", StringComparison.Ordinal) == true)
                    .OrderBy(t => t.FullName, StringComparer.Ordinal);
        }

        private static Assembly PersistenceAssembly() =>
            typeof(PlayGround.Persistence.Database.Generated.Soccer.Procedures.UspGetSoccerMatchDetail).Assembly;

        private static Type ResolveType(string typeName) =>
            PersistenceAssembly().GetType(typeName)
            ?? throw new InvalidOperationException($"{typeName} 을 찾지 못했다");

        /// <summary>생성 객체에서 대상 DB·프로시저 이름·파라미터 이름을 뽑는다.</summary>
        private static (DatabaseTypes Database, string Procedure, HashSet<string> Parameters) Describe(Type type)
        {
            // ProcedureBase(RepositoryBase)는 생성자에서 repository를 건드리지 않아 null로 만들 수 있다
            var instance = (ProcedureBase)Activator.CreateInstance(type, [null!])!;

            string procedure = Unbracket(instance.Procedure);
            DynamicParameters parameters = instance.BuildParameters();
            var names = parameters.ParameterNames
                .Select(n => n.TrimStart('@'))
                // @ReturnValue는 SQL 파라미터가 아니라 프로시저 RETURN 값을 받는 Dapper 자리다
                .Where(n => !n.Equals(ReturnValueParameter, StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 네임스페이스가 대상 DB를 가른다 (…Generated.Soccer.Procedures / …Generated.Account.Procedures)
            DatabaseTypes database = type.Namespace?.Contains(".Account.", StringComparison.Ordinal) == true
                ? DatabaseTypes.Account
                : DatabaseTypes.Soccer;

            return (database, procedure, names);
        }

        /// <summary>"[dbo].[UspX]" → "UspX".</summary>
        private static string Unbracket(string qualified)
        {
            string last = qualified.Split('.').Last();
            return last.Trim('[', ']');
        }

        //.// DB 조회

        private static Dictionary<string, HashSet<string>> LoadParameters(DatabaseTypes database)
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            using var connection = new SqlConnection(LocalDatabaseFixture.ConnectionStringFor(database));
            connection.Open();

            using SqlCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT p.name AS ProcedureName, ISNULL(pa.name, '') AS ParameterName
                FROM sys.procedures p
                LEFT JOIN sys.parameters pa ON pa.object_id = p.object_id
                WHERE p.is_ms_shipped = 0
                """;

            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string procedure = reader.GetString(0);
                string parameter = reader.GetString(1).TrimStart('@');

                if (!map.TryGetValue(procedure, out HashSet<string>? names))
                {
                    names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    map[procedure] = names;
                }

                if (parameter.Length > 0)
                {
                    names.Add(parameter);
                }
            }

            return map;
        }
    }
}
