using Microsoft.Extensions.Configuration;
using PlayGround.Infrastructure.Database;
using Xunit;

namespace PlayGround.Tests.Infrastructure
{
    public class DatabaseConfigurationTests
    {
        private static DatabaseConfiguration Bind(Dictionary<string, string?> values)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var result = configuration
                .GetSection(DatabaseConfiguration.Section)
                .Get<DatabaseConfiguration>();

            Assert.NotNull(result);
            return result!;
        }

        [Fact]
        public void Binds_AccountAndSoccer_FromStringKeys()
        {
            var config = Bind(new Dictionary<string, string?>
            {
                ["DatabaseConfiguration:Databases:Account:ConnectionString"] = "Server=a;Database=PlayGround_Account;",
                ["DatabaseConfiguration:Databases:Soccer:ConnectionString"] = "Server=s;Database=PlayGround_Soccer;"
            });

            Assert.True(config.HasDatabase(DatabaseTypes.Account));
            Assert.True(config.HasDatabase(DatabaseTypes.Soccer));
            Assert.Equal(2, config.Databases.Count);
        }

        [Fact]
        public void Provider_DefaultsToSqlServer()
        {
            var config = Bind(new Dictionary<string, string?>
            {
                ["DatabaseConfiguration:Databases:Account:ConnectionString"] = "Server=a;Database=Acc;"
            });

            Assert.Equal(DatabaseProvider.SqlServer, config.GetDatabaseOptions(DatabaseTypes.Account).Provider);
        }

        [Fact]
        public void GetProviderConnection_AppendsPoolParams_PerDatabase()
        {
            var config = Bind(new Dictionary<string, string?>
            {
                ["DatabaseConfiguration:Databases:Account:ConnectionString"] = "Server=a;Database=Acc;",
                ["DatabaseConfiguration:Databases:Soccer:ConnectionString"] = "Server=s;Database=Soc;"
            });

            var account = config.GetProviderConnection(DatabaseTypes.Account);
            var soccer = config.GetProviderConnection(DatabaseTypes.Soccer);

            // 각 DB가 자기 커넥션 문자열을 유지 (교차 오염 없음)
            Assert.Contains("Database=Acc", account.Connection);
            Assert.Contains("Database=Soc", soccer.Connection);
            // GetConnectionString()이 풀 파라미터를 덧붙임
            Assert.Contains("Max Pool Size", account.Connection);
        }

        [Fact]
        public void GetDatabaseOptions_Unconfigured_Throws()
        {
            var config = Bind(new Dictionary<string, string?>
            {
                ["DatabaseConfiguration:Databases:Account:ConnectionString"] = "Server=a;Database=Acc;"
            });

            Assert.Throws<InvalidOperationException>(() => config.GetDatabaseOptions(DatabaseTypes.Soccer));
        }
    }
}
