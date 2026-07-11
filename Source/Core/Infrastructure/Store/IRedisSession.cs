using StackExchange.Redis;

namespace PlayGround.Infrastructure.Store
{
    /// <summary>
    /// Redis 세션 인터페이스
    /// </summary>
    public interface IRedisSession : IAsyncDisposable
    {
        IDatabase Database { get; }
        bool IsConnected { get; }

        #region String

        Task<RedisResult<string>> TryStringGetAsync(string key);
        Task<RedisResult<T>> TryGetAsync<T>(string key);

        Task<RedisResult<bool>> TryStringSetAsync(string key, string value, TimeSpan? expiry = null);
        Task<RedisResult<bool>> TrySetAsync<T>(string key, T value, TimeSpan? expiry = null);

        #endregion

        #region Hash

        Task<RedisResult<bool>> TryHashSetAsync<TField, TValue>(string key, TField hashField, TValue value);
        Task<RedisResult<TValue>> TryHashGetAsync<TField, TValue>(string key, TField hashField);
        Task<RedisResult<Dictionary<string, TValue>>> TryHashAllGetAsync<TValue>(string key);

        #endregion

        #region Key

        Task<RedisResult<bool>> TryKeyExistsAsync(string key);
        Task<RedisResult<bool>> TryKeyDeleteAsync(string key);
        Task<RedisResult<TimeSpan?>> TryGetExpiryRemainingAsync(string key);
        Task<bool> SetExpiryAsync(string key, TimeSpan expiry);

        #endregion

        /// <summary>
        /// 연결 상태 확인 (헬스체크)
        /// </summary>
        Task<bool> PingAsync();
    }
}
