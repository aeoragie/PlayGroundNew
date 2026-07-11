namespace PlayGround.Shared.Result
{
    public static class ResultBuilderExtensions
    {
        public static ResultBuilder<T> CreateBuilder<T>() => new();
        public static ResultBuilder CreateBuilder() => new();

        public static ResultBuilder<T> ToBuilder<T>(this T value)
        {
            return new ResultBuilder<T>().WithValue(value);
        }

        public static ResultBuilder<T> ToBuilder<T>(this Exception exception, ErrorCode? errorCode = null)
        {
            return new ResultBuilder<T>().WithException(exception, errorCode);
        }
    }
}
