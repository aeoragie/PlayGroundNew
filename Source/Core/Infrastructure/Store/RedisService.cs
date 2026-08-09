using PlayGround.Shared.Primitives;
using PlayGround.Shared.Result;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog;
using PlayGround.Infrastructure.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PlayGround.Infrastructure.Store
{
    public class RedisConfig
    {
        public static readonly string Section = "RedisConfig";
        public List<RedisConnectionConfig> Connections { get; set; } = new();
    }

    public class RedisConnectionConfig
    {
        public string Name { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public int DatabaseId { get; set; } = 0;
    }

    public class RedisService : IHostedService, IAsyncDisposable
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly IConfiguration mConfiguration;
        private readonly ConcurrentDictionary<string, RedisConnectionEntry> Connections = new();

        public RedisService(IConfiguration configuration)
        {
            mConfiguration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var config = mConfiguration.GetSection(RedisConfig.Section).Get<RedisConfig>();
            if (config?.Connections == null || config.Connections.Count == 0)
            {
                Logger.WarnWith("No Redis connections configured");
                return;
            }

            foreach (var connConfig in config.Connections)
            {
                if (string.IsNullOrEmpty(connConfig.Name))
                {
                    Panic.Fail("Redis connection name is required.");
                }

                // 파싱 예외로 시끄럽게 실패시키지 않고 건너뛴다 (로컬 개발 기본값).
                if (string.IsNullOrWhiteSpace(connConfig.ConnectionString))
                {
                    Logger.WarnWith("Redis connection string is empty — skipped", ("Name", connConfig.Name));
                    continue;
                }

                try
                {
                    var options = ConfigurationOptions.Parse(connConfig.ConnectionString);

                    // 기동 시점에 Redis가 죽어 있어도 멀티플렉서를 만들고 **뒤에서 계속 재연결**한다.
                    // 기본값(AbortOnConnectFail=true)이면 여기서 예외가 나고 연결이 등록되지 않아,
                    // Redis가 살아나도 앱을 재시작하기 전까지 영영 붙지 않는다(배포 중 블립 한 번이면 그렇게 된다).
                    if (!connConfig.ConnectionString.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
                    {
                        options.AbortOnConnectFail = false;
                    }

                    var multiplexer = await ConnectionMultiplexer.ConnectAsync(options);

                    var entry = new RedisConnectionEntry(multiplexer, connConfig.DatabaseId);
                    if (!Connections.TryAdd(connConfig.Name, entry))
                    {
                        Logger.WarnWith("Redis connection already exists", ("Name", connConfig.Name));
                        await multiplexer.DisposeAsync();
                        continue;
                    }

                    Logger.InfoWith("Redis connection established", ("Name", connConfig.Name));
                }
                catch (Exception ex)
                {
                    Logger.ErrorWith(ex, "Redis connection failed", ("Name", connConfig.Name));
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await DisposeAsync();
        }

        public IRedisSession? CreateSession(string connectionName)
        {
            if (!Connections.TryGetValue(connectionName, out var entry))
            {
                Logger.WarnWith("Redis connection not found", ("Name", connectionName));
                return null;
            }

            if (!entry.Multiplexer.IsConnected)
            {
                Logger.WarnWith("Redis connection is not connected", ("Name", connectionName));
                return null;
            }

            return new RedisSession(entry.Multiplexer, entry.DatabaseId);
        }

        public async Task<Result<T>> WithSessionAsync<T>(string connectionName, Func<IRedisSession, Task<T>> action)
        {
            await using var session = CreateSession(connectionName);
            if (session is null)
            {
                return Result<T>.Error(ErrorCode.CacheUnavailable, $"Redis connection '{connectionName}' is not available.");
            }

            return Result<T>.Success(await action(session));
        }

        public bool IsConnected(string connectionName)
        {
            return Connections.TryGetValue(connectionName, out var entry)
                && entry.Multiplexer.IsConnected;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var entry in Connections.Values)
            {
                try
                {
                    await entry.Multiplexer.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Logger.WarnWith(ex, "Failed to dispose Redis connection");
                }
            }

            Connections.Clear();
            Logger.InfoWith("All Redis connections disposed");
        }

        private record RedisConnectionEntry(IConnectionMultiplexer Multiplexer, int DatabaseId);
    }
}
