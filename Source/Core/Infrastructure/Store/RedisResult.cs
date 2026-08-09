using PlayGround.Shared.Result;

namespace PlayGround.Infrastructure.Store
{
    public class RedisResult<T>
    {
        private readonly Result<T> mInnerResult;

        public bool IsSuccess => mInnerResult.IsSuccess;
        public bool IsError => mInnerResult.IsError;
        public bool HasValue { get; }
        public T? Value => HasValue ? mInnerResult.Value : default;
        public ResultInfo ResultData => mInnerResult.ResultData;
        public string Message => mInnerResult.Message;

        private RedisResult(Result<T> result, bool hasValue)
        {
            mInnerResult = result;
            HasValue = hasValue;
        }

        public static RedisResult<T> Ok(T? value)
        {
            return new RedisResult<T>(Result<T>.Success(value!), value is not null);
        }

        public static RedisResult<T> Empty()
        {
            return new RedisResult<T>(Result<T>.Success(default!), false);
        }

        public static RedisResult<T> Fail()
        {
            return new RedisResult<T>(Result<T>.Error(ErrorCode.CacheError), false);
        }

        public static RedisResult<T> Fail(ErrorCode code)
        {
            return new RedisResult<T>(Result<T>.Error(code), false);
        }

        public static RedisResult<T> Fail(Exception ex)
        {
            return new RedisResult<T>(Result<T>.FromException(ex, ErrorCode.CacheError), false);
        }
    }
}
