using Microsoft.Extensions.Logging;

namespace PlayGround.Shared.Logging
{
    /// <summary>
    /// 사람이 읽는 문장과 구조화 속성을 동시에 남긴다. 식별자는 문자열 보간이 아니라 이 필드로 넘긴다.
    /// 예) <c>Logger.InfoWith("Team created", ("TeamId", id))</c>
    /// </summary>
    public static class KeyValueLogExtensions
    {
        public static void TraceWith(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Trace, null, message, fields);

        public static void DebugWith(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Debug, null, message, fields);

        public static void InfoWith(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Information, null, message, fields);

        public static void WarnWith(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Warning, null, message, fields);

        public static void WarnWith(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Warning, exception, message, fields);

        public static void ErrorWith(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Error, null, message, fields);

        public static void ErrorWith(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Error, exception, message, fields);

        public static void FatalWith(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Critical, null, message, fields);

        public static void FatalWith(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Critical, exception, message, fields);

        private static void Write(ILogger logger, LogLevel level, Exception? exception, string message, (string Key, object? Value)[] fields)
        {
            if (logger is null || !logger.IsEnabled(level))
            {
                return;
            }

            string template = LogFieldFormatter.BuildTemplate(message, fields);
            object?[] values = new object?[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                values[i] = fields[i].Value;
            }

#pragma warning disable CA2254 // 템플릿을 필드에서 조립한다
            logger.Log(level, exception, template, values);
#pragma warning restore CA2254
        }
    }
}
