namespace PlayGround.Shared.Result
{
    public static class ResultAsync
    {
        public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation, ErrorCode? errorCode = null)
        {
            try
            {
                var result = await operation();
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<T>.FromException(ex, errorCode);
            }
        }
    }
}
