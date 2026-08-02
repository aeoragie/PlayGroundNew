using Microsoft.Data.SqlClient;
using PlayGround.Infrastructure.Database;

using Xunit;

namespace PlayGround.Tests.Infrastructure.Database
{
    /// <summary>
    /// 로컬 개발 DB 연결. **없으면 테스트를 실패시키지 않고 건너뛴다** —
    /// CI·새 clone에는 DB가 없고, 그때 빨개지면 진짜 실패와 구분이 안 된다.
    ///
    /// 커넥션 문자열 우선순위: 환경변수 → 개발 기본값(.\SQLEXPRESS).
    /// 환경변수는 `PLAYGROUND_TEST_SOCCER_CONNSTR` · `PLAYGROUND_TEST_ACCOUNT_CONNSTR`.
    /// </summary>
    public sealed class LocalDatabaseFixture
    {
        private const string DevelopmentSoccer =
            @"Server=.\SQLEXPRESS;Database=PlayGround_Soccer;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

        private const string DevelopmentAccount =
            @"Server=.\SQLEXPRESS;Database=PlayGround_Account;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

        /// <summary>연결 불가 사유 — null이면 사용 가능.</summary>
        public string? UnavailableReason { get; }

        public LocalDatabaseFixture()
        {
            UnavailableReason = Probe();
        }

        public bool IsAvailable => UnavailableReason is null;

        public static string ConnectionStringFor(DatabaseTypes database) => database switch
        {
            DatabaseTypes.Account =>
                Environment.GetEnvironmentVariable("PLAYGROUND_TEST_ACCOUNT_CONNSTR") ?? DevelopmentAccount,
            _ =>
                Environment.GetEnvironmentVariable("PLAYGROUND_TEST_SOCCER_CONNSTR") ?? DevelopmentSoccer,
        };

        /// <summary>연결이 안 되면 테스트를 건너뛴다(실패가 아니다).</summary>
        public void SkipIfUnavailable()
        {
            Assert.SkipWhen(!IsAvailable, $"로컬 DB를 쓸 수 없어 건너뜁니다 — {UnavailableReason}");
        }

        public SqlConnection Open(DatabaseTypes database)
        {
            var connection = new SqlConnection(ConnectionStringFor(database));
            connection.Open();
            return connection;
        }

        private static string? Probe()
        {
            foreach (DatabaseTypes database in new[] { DatabaseTypes.Account, DatabaseTypes.Soccer })
            {
                try
                {
                    using var connection = new SqlConnection(ConnectionStringFor(database));
                    connection.Open();
                }
                catch (Exception ex)
                {
                    return $"{database}: {ex.Message}";
                }
            }

            return null;
        }
    }

    /// <summary>DB를 쓰는 테스트는 이 컬렉션으로 묶어 커넥션 탐색을 한 번만 한다.</summary>
    [CollectionDefinition(Name)]
    public sealed class LocalDatabaseCollection : ICollectionFixture<LocalDatabaseFixture>
    {
        public const string Name = "LocalDatabase";
    }
}
