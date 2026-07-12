using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog;
using StackExchange.Redis;

namespace PlayGround.Infrastructure.Store
{
    /// <summary>
    /// Redis 설정 (appsettings.json)
    /// </summary>
    public class RedisConfig
    {
        public static readonly string Section = "RedisConfig";
        public List<RedisConnectionConfig> Connections { get; set; } = new();
    }

    /// <summary>
    /// 개별 Redis 연결 설정
    /// </summary>
    public class RedisConnectionConfig
    {
        public string Name { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public int DatabaseId { get; set; } = 0;
    }

    /// <summary>
    /// Redis 연결 생명주기 관리 및 세션 생성
    /// </summary>
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
                Logger.Warn("No Redis connections configured.");
                return;
            }

            foreach (var connConfig in config.Connections)
            {
                Debug.Assert(!string.IsNullOrEmpty(connConfig.Name), "Redis connection name is required");
                if (string.IsNullOrEmpty(connConfig.Name))
                {
                    continue;
                }

                try
                {
                    var options = ConfigurationOptions.Parse(connConfig.ConnectionString);
                    var multiplexer = await ConnectionMultiplexer.ConnectAsync(options);

                    var entry = new RedisConnectionEntry(multiplexer, connConfig.DatabaseId);
                    if (!Connections.TryAdd(connConfig.Name, entry))
                    {
                        Logger.Warn("Redis connection already exists. {{ Name:{Name} }}", connConfig.Name);
                        await multiplexer.DisposeAsync();
                        continue;
                    }

                    Logger.Info("Redis connection established. {{ Name:{Name} }}", connConfig.Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Redis connection failed. {{ Name:{Name} }}", connConfig.Name);
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await DisposeAsync();
        }

        /// <summary>
        /// 이름으로 Redis 세션 생성
        /// </summary>
        public IRedisSession? CreateSession(string connectionName)
        {
            if (!Connections.TryGetValue(connectionName, out var entry))
            {
                Logger.Warn("Redis connection not found. {{ Name:{Name} }}", connectionName);
                return null;
            }

            if (!entry.Multiplexer.IsConnected)
            {
                Logger.Warn("Redis connection is not connected. {{ Name:{Name} }}", connectionName);
                return null;
            }

            return new RedisSession(entry.Multiplexer, entry.DatabaseId);
        }

        /// <summary>
        /// 안전한 세션 사용 (acquire → use → dispose)
        /// </summary>
        public async Task<T> WithSessionAsync<T>(string connectionName, Func<IRedisSession, Task<T>> action)
        {
            await using var session = CreateSession(connectionName)
                ?? throw new InvalidOperationException($"Redis connection '{connectionName}' is not available");

            return await action(session);
        }

        /// <summary>
        /// 연결 상태 확인
        /// </summary>
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
                    Logger.Warn(ex, "Failed to dispose Redis connection.");
                }
            }

            Connections.Clear();
            Logger.Info("All Redis connections disposed.");
        }

        private record RedisConnectionEntry(IConnectionMultiplexer Multiplexer, int DatabaseId);
    }
}
