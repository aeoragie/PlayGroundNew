using PlayGround.Shared.Result;

namespace PlayGround.Infrastructure.Store
{
    /// <summary>
    /// Redis 작업 결과 (성공/실패/빈값)
    /// </summary>
    public class RedisResult<T>
    {
        private readonly Result<T> InnerResult;

        public bool IsSuccess => InnerResult.IsSuccess;
        public bool IsError => InnerResult.IsError;
        public bool HasValue { get; }
        public T? Value => InnerResult.Value;
        public ResultInfo ResultData => InnerResult.ResultData;
        public string Message => InnerResult.Message;

        private RedisResult(Result<T> result, bool hasValue)
        {
            InnerResult = result;
            HasValue = hasValue;
        }

        /// <summary>
        /// 키 조회 성공 (값 없을 경우 HasValue = false)
        /// </summary>
        public static RedisResult<T> Ok(T? value)
        {
            return new RedisResult<T>(Result<T>.Success(value!), value is not null);
        }

        /// <summary>
        /// 키 미존재 (정상 조회, 빈 결과)
        /// </summary>
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
