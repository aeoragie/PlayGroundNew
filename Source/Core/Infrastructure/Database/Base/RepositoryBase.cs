using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Dapper;
using NLog;
using PlayGround.Shared.Result;

namespace PlayGround.Infrastructure.Database.Base;

public abstract class RepositoryBase
{
    protected readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly DatabaseConfiguration Configuration;

    public abstract DatabaseTypes Database { get; }

    protected DatabaseOptions Options => Configuration.GetDatabaseOptions(Database);

    protected RepositoryBase(IOptions<DatabaseConfiguration> options)
    {
        Configuration = options.Value;
    }

    #region Connection Management

    public DbConnection CreateConnection()
    {
        return CreateConnection(Database);
    }

    public DbConnection CreateConnection(DatabaseTypes databaseType)
    {
        var pair = Configuration.GetProviderConnection(databaseType);
        return pair.Provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnection(pair.Connection),
            DatabaseProvider.MySql => throw new NotImplementedException("MySQL provider is not implemented yet."),
            DatabaseProvider.PostgreSql => throw new NotImplementedException("PostgreSQL provider is not implemented yet."),
            _ => throw new NotSupportedException($"Database provider {pair.Provider} is not supported.")
        };
    }

    protected virtual async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellation = default)
    {
        var connection = CreateConnection();
        await connection.OpenAsync(cancellation);
        Logger.Trace("Connection opened. {{ Database:{Database} }}", Database);
        return connection;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            stopwatch.Stop();
            Logger.Debug("Connection test succeeded. {{ Database:{Database}, ElapsedMs:{ElapsedMs} }}", Database, stopwatch.ElapsedMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Connection test failed. {{ Database:{Database}, ElapsedMs:{ElapsedMs} }}", Database, stopwatch.ElapsedMilliseconds);
            return false;
        }
    }

    #endregion

    #region Single Query

    public async Task<Result<TRow>> QuerySingleOrDefaultAsync<TRow>(
        string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var result = await connection.QuerySingleOrDefaultAsync<TRow>(sql, parameters, commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch.Stop();

            if (result == null)
            {
                Logger.Debug("Query returned no result. {{ ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", stopwatch.ElapsedMilliseconds, TruncateSql(sql));
                return Result<TRow>.Error(ErrorCode.NotFound, "No data found");
            }

            Logger.Debug("Query executed. {{ ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", stopwatch.ElapsedMilliseconds, TruncateSql(sql));
            return Result<TRow>.Success(result);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Transient SQL error. {{ ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", stopwatch.ElapsedMilliseconds, TruncateSql(sql));
            return Result<TRow>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Query failed. {{ ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", stopwatch.ElapsedMilliseconds, TruncateSql(sql));
            return Result<TRow>.FromException(ex);
        }
    }

    public async Task<Result<TRow>> ProcedureSingleOrDefaultAsync<TRow>(
        ProcedureBase procedure, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var result = await connection.QuerySingleOrDefaultAsync<TRow>(
                procedure.Procedure,
                procedure.BuildParameters(),
                commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch.Stop();

            if (result == null)
            {
                Logger.Debug("Procedure returned no result. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
                return Result<TRow>.Error(ErrorCode.NotFound, "No data found");
            }

            Logger.Debug("Procedure executed. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<TRow>.Success(result);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Transient SQL error. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<TRow>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Procedure failed. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<TRow>.FromException(ex);
        }
    }

    #endregion

    #region Multiple Query

    public async Task<Result<IEnumerable<TRow>>> QueryAsync<TRow>(
        string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var result = await connection.QueryAsync<TRow>(sql, parameters, commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch.Stop();

            var count = result.TryGetNonEnumeratedCount(out var c) ? c : -1;
            Logger.Debug("Query returned rows. {{ Count:{Count}, ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", count, stopwatch.ElapsedMilliseconds, TruncateSql(sql));

            return Result<IEnumerable<TRow>>.Success(result);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Transient SQL error. {{ ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", stopwatch.ElapsedMilliseconds, TruncateSql(sql));
            return Result<IEnumerable<TRow>>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Query failed. {{ ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", stopwatch.ElapsedMilliseconds, TruncateSql(sql));
            return Result<IEnumerable<TRow>>.FromException(ex);
        }
    }

    public async Task<Result<IEnumerable<TRow>>> ProcedureAsync<TRow>(
        ProcedureBase procedure, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var result = await connection.QueryAsync<TRow>(
                procedure.Procedure,
                procedure.BuildParameters(),
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch.Stop();
            var count = result.TryGetNonEnumeratedCount(out var c) ? c : -1;
            Logger.Debug("Procedure returned rows. {{ Count:{Count}, ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", count, stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<IEnumerable<TRow>>.Success(result);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Transient SQL error. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<IEnumerable<TRow>>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Procedure failed. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<IEnumerable<TRow>>.FromException(ex);
        }
    }

    public async Task<Result<MultiQueryReader>> ProcedureMultipleAsync(
        ProcedureBase procedure, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // 리더가 커넥션을 계속 사용하므로 여기서 dispose하지 않고 MultiQueryReader에 소유권을 넘긴다
        var connection = await OpenConnectionAsync(cancellation);
        try
        {
            var reader = await connection.QueryMultipleAsync(
                procedure.Procedure,
                procedure.BuildParameters(),
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch.Stop();
            Logger.Debug("Procedure multiple query executed. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<MultiQueryReader>.Success(new MultiQueryReader(connection, reader));
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch.Stop();
            await connection.DisposeAsync();
            Logger.Debug(ex, "Transient SQL error. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<MultiQueryReader>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await connection.DisposeAsync();
            Logger.Debug(ex, "Procedure multiple query failed. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<MultiQueryReader>.FromException(ex);
        }
    }

    #endregion

    #region Execute (Insert/Update/Delete)

    public async Task<Result<int>> ExecuteAsync(
        string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var affectedRows = await connection.ExecuteAsync(sql, parameters, commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch.Stop();
            Logger.Debug("Execute completed. {{ AffectedRows:{AffectedRows}, ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", affectedRows, stopwatch.ElapsedMilliseconds, TruncateSql(sql));
            return Result<int>.Success(affectedRows);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Transient SQL error. {{ ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", stopwatch.ElapsedMilliseconds, TruncateSql(sql));
            return Result<int>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Execute failed. {{ ElapsedMs:{ElapsedMs}, Sql:{Sql} }}", stopwatch.ElapsedMilliseconds, TruncateSql(sql));
            return Result<int>.FromException(ex);
        }
    }

    public async Task<Result<int>> ProcedureExecuteAsync(
        ProcedureBase procedure, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);

            var affectedRows = await connection.ExecuteAsync(
                procedure.Procedure,
                procedure.BuildParameters(),
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch.Stop();
            Logger.Debug("Procedure execute completed. {{ AffectedRows:{AffectedRows}, ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", affectedRows, stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<int>.Success(affectedRows);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Transient SQL error. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<int>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Debug(ex, "Procedure execute failed. {{ ElapsedMs:{ElapsedMs}, Procedure:{Procedure} }}", stopwatch.ElapsedMilliseconds, procedure.Procedure);
            return Result<int>.FromException(ex);
        }
    }

    #endregion

    #region Transaction Support

    public async Task<Result<TResult>> ExecuteInTransactionAsync<TResult>(
        Func<DbConnection, DbTransaction, Task<TResult>> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellation = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var connection = await OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellation);

        try
        {
            var result = await operation(connection, transaction);
            await transaction.CommitAsync(cancellation);

            stopwatch.Stop();
            Logger.Debug("Transaction committed. {{ ElapsedMs:{ElapsedMs} }}", stopwatch.ElapsedMilliseconds);
            return Result<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackEx)
            {
                Logger.Debug(rollbackEx, "Transaction rollback failed.");
            }

            Logger.Debug(ex, "Transaction rolled back. {{ ElapsedMs:{ElapsedMs} }}", stopwatch.ElapsedMilliseconds);
            return Result<TResult>.FromException(ex);
        }
    }

    public async Task<Result<int>> ExecuteInTransactionAsync(
        Func<DbConnection, DbTransaction, Task<int>> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellation = default)
    {
        return await ExecuteInTransactionAsync<int>(operation, isolationLevel, cancellation);
    }

    #endregion

    #region Retry Support

    public async Task<Result<TResult>> ExecuteWithRetryAsync<TResult>(
        Func<Task<Result<TResult>>> operation, int? maxRetries = null, CancellationToken cancellation = default)
    {
        var retryCount = maxRetries ?? Options.MaxRetryCount;
        var attempt = 0;
        while (true)
        {
            attempt++;

            var result = await operation();
            if (result.IsSuccess)
            {
                return result;
            }

            if (!result.ResultData.DetailCode.IsRetryable() || attempt >= retryCount)
            {
                if (attempt > 1)
                {
                    Logger.Debug("Operation failed after retries. {{ Attempts:{Attempts} }}", attempt);
                }
                return result;
            }

            var delay = Options.RetryDelayMilliseconds * (int)Math.Pow(2, attempt - 1);
            Logger.Debug("Retry scheduled. {{ Attempt:{Attempt}, MaxRetries:{MaxRetries}, DelayMs:{DelayMs} }}", attempt, retryCount, delay);
            await Task.Delay(delay, cancellation);
        }
    }

    #endregion

    #region Helper Methods

    // SQL Server transient error codes
    private static readonly FrozenSet<int> TransientErrorNumbers = new[]
    {
        -2,     // Timeout
        20,     // The instance of SQL Server does not support encryption
        64,     // Connection was successfully established but then an error occurred
        233,    // Connection initialization error
        10053,  // Connection forcibly closed
        10054,  // Connection reset by peer
        10060,  // Connection timeout
        40197,  // Service error processing request
        40501,  // Service busy
        40613,  // Database unavailable
        49918,  // Cannot process request (not enough resources)
        49919,  // Cannot process create/update request (too many operations)
        49920   // Cannot process request (too many operations)
    }.ToFrozenSet();

    private static bool IsTransientError(SqlException ex)
    {
        return TransientErrorNumbers.Contains(ex.Number);
    }

    private static string TruncateSql(string sql, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return string.Empty;
        }

        var normalized = sql.Replace("\r\n", " ").Replace("\n", " ").Trim();
        return normalized.Length <= maxLength ? normalized : string.Concat(normalized.AsSpan(0, maxLength), "...");
    }

    #endregion
}
