using PlayGround.Shared.Primitives;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace PlayGround.Infrastructure.Database;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly DatabaseConfiguration mConfiguration;

    public DatabaseHealthCheck(IOptions<DatabaseConfiguration> options)
    {
        mConfiguration = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellation = default)
    {
        if (mConfiguration.Databases == null || mConfiguration.Databases.Count == 0)
        {
            return HealthCheckResult.Healthy("No databases configured.");
        }

        var healthData = new Dictionary<string, object>();
        var unhealthyDatabases = new List<string>();

        foreach (var database in mConfiguration.Databases.Keys)
        {
            try
            {
                var pair = mConfiguration.GetProviderConnection(database);
                await using DbConnection connection = pair.Provider switch
                {
                    DatabaseProvider.SqlServer => new SqlConnection(pair.Connection),
                    _ => Panic.Fail<DbConnection>($"Database provider {pair.Provider} is not implemented.")
                };

                await connection.OpenAsync(cancellation);
                healthData[database.ToString()] = "Healthy";
            }
            catch (Exception ex)
            {
                healthData[database.ToString()] = $"Unhealthy: {ex.Message}";
                unhealthyDatabases.Add(database.ToString());
            }
        }

        if (unhealthyDatabases.Count > 0)
        {
            return HealthCheckResult.Unhealthy(
                $"Database connection failed: {string.Join(", ", unhealthyDatabases)}", data: healthData);
        }

        return HealthCheckResult.Healthy("All databases are healthy.", data: healthData);
    }
}
