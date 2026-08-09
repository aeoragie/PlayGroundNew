using NLog;
using PlayGround.Infrastructure.Logging;
using PlayGround.Shared.Primitives;
using StackExchange.Redis;
using System.Text.Json;

namespace PlayGround.Infrastructure.Store
{
    public class RedisSession : IRedisSession
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly IConnectionMultiplexer mMultiplexer;

        public IDatabase Database { get; }
        public bool IsConnected => mMultiplexer.IsConnected;

        public RedisSession(IConnectionMultiplexer multiplexer, int databaseId = 0)
        {
            if (multiplexer is null)
            {
                Panic.Fail("RedisSession requires a multiplexer.");
            }

            mMultiplexer = multiplexer;
            Database = mMultiplexer.GetDatabase(databaseId);
        }

        #region String

        public async Task<RedisResult<string>> TryStringGetAsync(string key)
        {
            try
            {
                if (!IsConnected)
                {
                    return RedisResult<string>.Fail();
                }

                var value = await Database.StringGetAsync(key);
                if (value.IsNullOrEmpty)
                {
                    return RedisResult<string>.Empty();
                }

                return RedisResult<string>.Ok(value!);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryStringGetAsync failed", ("Key", key));
                return RedisResult<string>.Fail(ex);
            }
        }

        public async Task<RedisResult<T>> TryGetAsync<T>(string key)
        {
            try
            {
                var result = await TryStringGetAsync(key);
                if (result.IsError)
                {
                    return RedisResult<T>.Fail();
                }

                if (!result.HasValue || result.Value is null)
                {
                    return RedisResult<T>.Empty();
                }

                return RedisResult<T>.Ok(JsonSerializer.Deserialize<T>(result.Value));
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryGetAsync failed", ("Key", key));
                return RedisResult<T>.Fail(ex);
            }
        }

        public async Task<RedisResult<bool>> TryStringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                if (!IsConnected)
                {
                    return RedisResult<bool>.Fail();
                }

                var result = expiry.HasValue
                    ? await Database.StringSetAsync(key, value, new Expiration(expiry.Value))
                    : await Database.StringSetAsync(key, value);
                return RedisResult<bool>.Ok(result);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryStringSetAsync failed", ("Key", key));
                return RedisResult<bool>.Fail(ex);
            }
        }

        public async Task<RedisResult<bool>> TrySetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                return await TryStringSetAsync(key, json, expiry);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TrySetAsync failed", ("Key", key));
                return RedisResult<bool>.Fail(ex);
            }
        }

        #endregion

        #region Hash

        public async Task<RedisResult<bool>> TryHashSetAsync<TField, TValue>(string key, TField hashField, TValue value)
        {
            try
            {
                if (!IsConnected)
                {
                    return RedisResult<bool>.Fail();
                }

                var field = JsonSerializer.Serialize(hashField);
                var json = JsonSerializer.Serialize(value);
                await Database.HashSetAsync(key, field, json);
                return RedisResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryHashSetAsync failed", ("Key", key), ("Field", hashField));
                return RedisResult<bool>.Fail(ex);
            }
        }

        public async Task<RedisResult<TValue>> TryHashGetAsync<TField, TValue>(string key, TField hashField)
        {
            try
            {
                if (!IsConnected)
                {
                    return RedisResult<TValue>.Fail();
                }

                var field = JsonSerializer.Serialize(hashField);
                var value = await Database.HashGetAsync(key, field);
                if (value.IsNull)
                {
                    return RedisResult<TValue>.Empty();
                }

                var deserialized = JsonSerializer.Deserialize<TValue>((string)value!);
                return deserialized is null
                    ? RedisResult<TValue>.Empty()
                    : RedisResult<TValue>.Ok(deserialized);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryHashGetAsync failed", ("Key", key), ("Field", hashField));
                return RedisResult<TValue>.Fail(ex);
            }
        }

        public async Task<RedisResult<Dictionary<string, TValue>>> TryHashAllGetAsync<TValue>(string key)
        {
            try
            {
                if (!IsConnected)
                {
                    return RedisResult<Dictionary<string, TValue>>.Fail();
                }

                var entries = await Database.HashGetAllAsync(key);
                var dict = new Dictionary<string, TValue>(entries.Length);

                foreach (var entry in entries)
                {
                    if (entry.Value.IsNullOrEmpty)
                    {
                        continue;
                    }

                    var value = JsonSerializer.Deserialize<TValue>((string)entry.Value!);
                    if (value is null)
                    {
                        continue;
                    }

                    dict[entry.Name!] = value;
                }

                return RedisResult<Dictionary<string, TValue>>.Ok(dict);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryHashAllGetAsync failed", ("Key", key));
                return RedisResult<Dictionary<string, TValue>>.Fail(ex);
            }
        }

        #endregion

        #region Key

        public async Task<RedisResult<bool>> TryKeyExistsAsync(string key)
        {
            try
            {
                if (!IsConnected)
                {
                    return RedisResult<bool>.Fail();
                }

                var exists = await Database.KeyExistsAsync(key);
                return RedisResult<bool>.Ok(exists);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryKeyExistsAsync failed", ("Key", key));
                return RedisResult<bool>.Fail(ex);
            }
        }

        public async Task<RedisResult<bool>> TryKeyDeleteAsync(string key)
        {
            try
            {
                if (!IsConnected)
                {
                    return RedisResult<bool>.Fail();
                }

                var deleted = await Database.KeyDeleteAsync(key);
                return RedisResult<bool>.Ok(deleted);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryKeyDeleteAsync failed", ("Key", key));
                return RedisResult<bool>.Fail(ex);
            }
        }

        public async Task<RedisResult<TimeSpan?>> TryGetExpiryRemainingAsync(string key)
        {
            try
            {
                if (!IsConnected)
                {
                    return RedisResult<TimeSpan?>.Fail();
                }

                var ttl = await Database.KeyTimeToLiveAsync(key);
                return RedisResult<TimeSpan?>.Ok(ttl);
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "TryGetExpiryRemainingAsync failed", ("Key", key));
                return RedisResult<TimeSpan?>.Fail(ex);
            }
        }

        public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry)
        {
            return await Database.KeyExpireAsync(key, expiry);
        }

        #endregion

        public async Task<bool> PingAsync()
        {
            try
            {
                var latency = await Database.PingAsync();
                return latency.TotalMilliseconds < 1000;
            }
            catch (Exception ex)
            {
                KeyValueLogExtensions.Warn(Logger, ex, "PingAsync failed");
                return false;
            }
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
