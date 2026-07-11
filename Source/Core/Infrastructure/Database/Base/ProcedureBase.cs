using Dapper;

namespace PlayGround.Infrastructure.Database.Base;

public abstract class ProcedureBase(RepositoryBase repository) : CommandBase(repository)
{
    public DynamicParameters Parameters { get; private set; } = new();

    public abstract string Procedure { get; }
    public abstract DynamicParameters BuildParameters();

    protected override string CommandName => Procedure;

    public virtual bool HasReturnValue() { return false; }
    public abstract int GetReturnValue();

    #region Execute (Insert/Update/Delete)

    public async Task<QueryResultBase> ExecuteAsync(int? commandTimeout = null, CancellationToken cancellation = default)
    {
        return await ExecuteCoreAsync(Repository.ProcedureExecuteAsync(this, commandTimeout, cancellation));
    }

    #endregion

    #region Single Query

    public async Task<QueryResultSingle<T>> SingleAsync<T>(int? commandTimeout = null, CancellationToken cancellation = default)
    {
        return await SingleCoreAsync<T>(Repository.ProcedureSingleOrDefaultAsync<T>(this, commandTimeout, cancellation));
    }

    #endregion

    #region Multiple Query

    public async Task<QueryResultList<T1>> QueryAsync<T1>(int? commandTimeout = null, CancellationToken cancellation = default)
    {
        return await QueryCoreAsync<T1>(Repository.ProcedureAsync<T1>(this, commandTimeout, cancellation));
    }

    #endregion
}
