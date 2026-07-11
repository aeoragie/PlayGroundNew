using System.Data.Common;
using Dapper;

namespace PlayGround.Infrastructure.Database.Base;

/// <summary>
/// 다중 결과셋 리더. GridReader와 함께 DbConnection의 소유권을 가져
/// Dispose 시 커넥션까지 반환한다 (GridReader만 닫으면 커넥션이 누수됨).
/// </summary>
public sealed class MultiQueryReader : IDisposable, IAsyncDisposable
{
    private readonly DbConnection mConnection;

    public SqlMapper.GridReader Reader { get; }
    public bool IsConsumed => Reader.IsConsumed;

    internal MultiQueryReader(DbConnection connection, SqlMapper.GridReader reader)
    {
        mConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public Task<IEnumerable<T>> ReadAsync<T>()
    {
        return Reader.ReadAsync<T>();
    }

    public Task<T?> ReadSingleOrDefaultAsync<T>()
    {
        return Reader.ReadSingleOrDefaultAsync<T?>();
    }

    public Task<T?> ReadFirstOrDefaultAsync<T>()
    {
        return Reader.ReadFirstOrDefaultAsync<T?>();
    }

    public void Dispose()
    {
        Reader.Dispose();
        mConnection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Reader.Dispose();
        await mConnection.DisposeAsync();
    }
}
