namespace PlayGround.Infrastructure.Database.Base;

public abstract class QueryBase(RepositoryBase repository) : CommandBase(repository)
{
    public abstract string Sql { get; }

    public virtual object? BuildParameters() { return null; }

    protected override string CommandName => Sql;

    #region Execute (Insert/Update/Delete)

    public async Task<QueryResultBase> ExecuteAsync(int? commandTimeout = null, CancellationToken cancellation = default)
    {
        return await ExecuteCoreAsync(Repository.ExecuteAsync(Sql, BuildParameters(), commandTimeout, cancellation));
    }

    #endregion

    #region Single Query

    public async Task<QueryResultSingle<T>> SingleAsync<T>(int? commandTimeout = null, CancellationToken cancellation = default)
    {
        return await SingleCoreAsync<T>(Repository.QuerySingleOrDefaultAsync<T>(Sql, BuildParameters(), commandTimeout, cancellation));
    }

    #endregion

    #region Multiple Query

    public async Task<QueryResultList<T1>> QueryAsync<T1>(int? commandTimeout = null, CancellationToken cancellation = default)
    {
        return await QueryCoreAsync<T1>(Repository.QueryAsync<T1>(Sql, BuildParameters(), commandTimeout, cancellation));
    }

    #endregion
}
