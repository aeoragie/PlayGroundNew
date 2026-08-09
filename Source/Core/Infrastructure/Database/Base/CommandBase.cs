using NLog;
using PlayGround.Infrastructure.Logging;
using PlayGround.Shared.Result;

namespace PlayGround.Infrastructure.Database.Base;

public abstract class CommandBase(RepositoryBase repository)
{
    protected readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    protected RepositoryBase Repository = repository;

    protected abstract string CommandName { get; }

    #region Execute (Insert/Update/Delete)

    protected async Task<QueryResultBase> ExecuteCoreAsync(Task<Result<int>> task)
    {
        var result = await task;
        if (result.IsError)
        {
            DiagnosticLog.Database(Logger, "Execute failed", ("Command", CommandName), ("Message", result.Message));
            return new QueryResultBase { ResultCode = QueryResult.Error };
        }

        DiagnosticLog.Database(Logger, "Execute completed", ("Command", CommandName), ("AffectedRows", result.Value));
        return new QueryResultBase { ResultCode = QueryResult.Success };
    }

    #endregion

    #region Single Query

    protected async Task<QueryResultSingle<T>> SingleCoreAsync<T>(Task<Result<T>> task)
    {
        var result = await task;

        var queryResult = new QueryResultSingle<T>();
        if (result.IsError)
        {
            if (result.ResultData.DetailCode == ErrorCode.NotFound)
            {
                DiagnosticLog.Database(Logger, "Single query returned no data", ("Command", CommandName));
            }
            else
            {
                DiagnosticLog.Database(Logger, "Single query failed", ("Command", CommandName), ("Message", result.Message));
            }

            queryResult.ResultCode = QueryResult.Error;
            return queryResult;
        }

        queryResult.SetValue(result.Value);
        return queryResult;
    }

    #endregion

    #region Multiple Query

    protected async Task<QueryResultList<T1>> QueryCoreAsync<T1>(Task<Result<IEnumerable<T1>>> task)
    {
        var result = await task;

        var queryResult = new QueryResultList<T1>();
        if (result.IsError)
        {
            DiagnosticLog.Database(Logger, "Query failed", ("Command", CommandName), ("Message", result.Message));
            queryResult.ResultCode = QueryResult.Error;
            return queryResult;
        }

        await queryResult.ReadResultsAsync(result.Value!);
        queryResult.ResultCode = QueryResult.Success;
        return queryResult;
    }

    #endregion
}
