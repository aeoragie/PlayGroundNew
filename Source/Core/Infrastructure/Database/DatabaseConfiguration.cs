using System.Collections.Concurrent;

namespace PlayGround.Infrastructure.Database;

public class DatabaseConfiguration
{
    public static readonly string Section = "DatabaseConfiguration";

    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<DatabaseTypes, DatabaseOptions> Databases { get; set; } = new();

    public record ProviderConnection(DatabaseProvider Provider, string Connection);

    // GetConnectionString()의 정규식 비용을 매 호출마다 지불하지 않기 위한 런타임 캐시 (설정 바인딩 대상 아님)
    private readonly ConcurrentDictionary<DatabaseTypes, ProviderConnection> mProviderConnections = new();

    public DatabaseOptions GetDatabaseOptions(DatabaseTypes database)
    {
        if (Databases.TryGetValue(database, out var options))
        {
            return options;
        }

        throw new InvalidOperationException($"Database configuration for {database} not found.");
    }

    public ProviderConnection GetProviderConnection(DatabaseTypes database)
    {
        return mProviderConnections.GetOrAdd(database, static (db, config) =>
        {
            var options = config.GetDatabaseOptions(db);
            return new ProviderConnection(options.Provider, options.GetConnectionString());
        }, this);
    }

    public bool HasDatabase(DatabaseTypes database)
    {
        return Databases.ContainsKey(database);
    }
}
