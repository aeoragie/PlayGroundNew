using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using NLog;
using PlayGround.Infrastructure.Logging;
using PlayGround.Shared.Primitives;
using PlayGround.Shared.Result;
using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace PlayGround.Infrastructure.Database.Base;

public abstract class RepositoryBase
{
    // 모든 쿼리가 이 클래스를 지나므로 Dapper 타입 핸들러 등록 지점으로 삼는다 (프로세스당 1회)
    static RepositoryBase()
    {
        SqlMapper.AddTypeHandler(new SystemTimeTypeHandler());
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    protected readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly DatabaseConfiguration mConfiguration;

    public abstract DatabaseTypes Database { get; }

    protected DatabaseOptions Options => mConfiguration.GetDatabaseOptions(Database);

    protected RepositoryBase(IOptions<DatabaseConfiguration> options)
    {
        mConfiguration = options.Value;
    }

    #region Connection Management

    public DbConnection CreateConnection()
    {
        return CreateConnection(Database);
    }

    public DbConnection CreateConnection(DatabaseTypes databaseType)
    {
        var pair = mConfiguration.GetProviderConnection(databaseType);
        return pair.Provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnection(pair.Connection),
            _ => Panic.Fail<DbConnection>($"Database provider {pair.Provider} is not implemented.")
        };
    }

    protected virtual async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellation = default)
    {
        var connection = CreateConnection();
        await connection.OpenAsync(cancellation);
        DiagDatabase("Connection opened");
        return connection;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            stopwatch?.Stop();
            DiagDatabase("Connection test succeeded", stopwatch);
            return true;
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            Logging.KeyValueLogExtensions.Error(Logger, ex, "Connection test failed", ("Database", Database), ("ElapsedMs", stopwatch?.ElapsedMilliseconds));
            return false;
        }
    }

    #endregion

    #region Single Query

    public async Task<Result<TRow>> QuerySingleOrDefaultAsync<TRow>(
        string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var result = await connection.QuerySingleOrDefaultAsync<TRow>(sql, parameters, commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch?.Stop();

            if (result == null)
            {
                DiagSql("Query returned no result", stopwatch, sql);
                return Result<TRow>.Error(ErrorCode.NotFound, "No data found");
            }

            DiagSql("Query executed", stopwatch, sql);
            return Result<TRow>.Success(result);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch?.Stop();
            return Result<TRow>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            return Result<TRow>.FromException(ex);
        }
    }

    public async Task<Result<TRow>> ProcedureSingleOrDefaultAsync<TRow>(
        ProcedureBase procedure, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var result = await connection.QuerySingleOrDefaultAsync<TRow>(
                procedure.Procedure,
                procedure.BuildParameters(),
                commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch?.Stop();

            if (result == null)
            {
                DiagProcedure("Procedure returned no result", stopwatch, procedure.Procedure);
                return Result<TRow>.Error(ErrorCode.NotFound, "No data found");
            }

            DiagProcedure("Procedure executed", stopwatch, procedure.Procedure);
            return Result<TRow>.Success(result);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch?.Stop();
            return Result<TRow>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            return Result<TRow>.FromException(ex);
        }
    }

    #endregion

    #region Multiple Query

    public async Task<Result<IEnumerable<TRow>>> QueryAsync<TRow>(
        string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var result = await connection.QueryAsync<TRow>(sql, parameters, commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch?.Stop();

            var count = result.TryGetNonEnumeratedCount(out var c) ? c : -1;
            DiagSql("Query returned rows", stopwatch, sql, count);

            return Result<IEnumerable<TRow>>.Success(result);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch?.Stop();
            return Result<IEnumerable<TRow>>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            return Result<IEnumerable<TRow>>.FromException(ex);
        }
    }

    public async Task<Result<IEnumerable<TRow>>> ProcedureAsync<TRow>(
        ProcedureBase procedure, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var result = await connection.QueryAsync<TRow>(
                procedure.Procedure,
                procedure.BuildParameters(),
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch?.Stop();
            var count = result.TryGetNonEnumeratedCount(out var c) ? c : -1;
            DiagProcedure("Procedure returned rows", stopwatch, procedure.Procedure, count);
            return Result<IEnumerable<TRow>>.Success(result);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch?.Stop();
            return Result<IEnumerable<TRow>>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            return Result<IEnumerable<TRow>>.FromException(ex);
        }
    }

    public async Task<Result<MultiQueryReader>> ProcedureMultipleAsync(
        ProcedureBase procedure, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();

        // 리더가 커넥션을 계속 사용하므로 여기서 dispose하지 않고 MultiQueryReader에 소유권을 넘긴다
        var connection = await OpenConnectionAsync(cancellation);
        try
        {
            var reader = await connection.QueryMultipleAsync(
                procedure.Procedure,
                procedure.BuildParameters(),
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch?.Stop();
            DiagProcedure("Procedure multiple query executed", stopwatch, procedure.Procedure);
            return Result<MultiQueryReader>.Success(new MultiQueryReader(connection, reader));
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch?.Stop();
            await connection.DisposeAsync();
            return Result<MultiQueryReader>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            await connection.DisposeAsync();
            return Result<MultiQueryReader>.FromException(ex);
        }
    }

    #endregion

    #region Execute (Insert/Update/Delete)

    public async Task<Result<int>> ExecuteAsync(
        string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);
            var affectedRows = await connection.ExecuteAsync(sql, parameters, commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch?.Stop();
            DiagSql("Execute completed", stopwatch, sql, affectedRows);
            return Result<int>.Success(affectedRows);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch?.Stop();
            return Result<int>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            return Result<int>.FromException(ex);
        }
    }

    public async Task<Result<int>> ProcedureExecuteAsync(
        ProcedureBase procedure, int? commandTimeout = null, CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();
        try
        {
            await using var connection = await OpenConnectionAsync(cancellation);

            var affectedRows = await connection.ExecuteAsync(
                procedure.Procedure,
                procedure.BuildParameters(),
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? Options.CommandTimeout);

            stopwatch?.Stop();
            DiagProcedure("Procedure execute completed", stopwatch, procedure.Procedure, affectedRows);
            return Result<int>.Success(affectedRows);
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            stopwatch?.Stop();
            return Result<int>.Error(ErrorCode.DatabaseTimeout, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            return Result<int>.FromException(ex);
        }
    }

    #endregion

    #region Transaction Support

    public async Task<Result<TResult>> ExecuteInTransactionAsync<TResult>(
        Func<DbConnection, DbTransaction, Task<TResult>> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellation = default)
    {
        Stopwatch? stopwatch = DiagnosticLog.DatabaseTimer();
        await using var connection = await OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellation);

        try
        {
            var result = await operation(connection, transaction);
            await transaction.CommitAsync(cancellation);

            stopwatch?.Stop();
            DiagDatabase("Transaction committed", stopwatch);
            return Result<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackEx)
            {
                Logging.KeyValueLogExtensions.Error(Logger, rollbackEx, "Transaction rollback failed", ("Database", Database));
            }

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
                    Logging.KeyValueLogExtensions.Warn(Logger, "Operation failed after retries", ("Database", Database), ("Attempts", attempt));
                }
                return result;
            }

            var delay = Options.RetryDelayMilliseconds * (int)Math.Pow(2, attempt - 1);
            DiagRetry(attempt, retryCount, delay);
            await Task.Delay(delay, cancellation);
        }
    }

    #endregion

    #region Helper Methods

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

    [Conditional("LOG_DATABASE")]
    private void DiagRetry(int attempt, int maxRetries, int delayMs)
    {
        KeyValueLogExtensions.Debug(Logger, "Retry scheduled", ("Attempt", attempt), ("MaxRetries", maxRetries), ("DelayMs", delayMs));
    }

    [Conditional("LOG_DATABASE")]
    private void DiagDatabase(string message, Stopwatch? stopwatch = null)
    {
        KeyValueLogExtensions.Debug(Logger, message, ("Database", Database), ("ElapsedMs", stopwatch?.ElapsedMilliseconds));
    }

    [Conditional("LOG_DATABASE")]
    private void DiagSql(string message, Stopwatch? stopwatch, string sql, long? rows = null)
    {
        KeyValueLogExtensions.Debug(Logger, message, ("ElapsedMs", stopwatch?.ElapsedMilliseconds), ("Rows", rows), ("Sql", TruncateSql(sql)));
    }

    [Conditional("LOG_DATABASE")]
    private void DiagProcedure(string message, Stopwatch? stopwatch, string procedure, long? rows = null)
    {
        KeyValueLogExtensions.Debug(Logger, message, ("ElapsedMs", stopwatch?.ElapsedMilliseconds), ("Rows", rows), ("Procedure", procedure));
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
