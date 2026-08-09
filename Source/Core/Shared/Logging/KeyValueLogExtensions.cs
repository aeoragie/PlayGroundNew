using Microsoft.Extensions.Logging;

namespace PlayGround.Shared.Logging
{
    public static class KeyValueLogExtensions
    {
        public static void Trace(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Trace, null, message, fields);

        public static void Debug(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Debug, null, message, fields);

        public static void Info(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Information, null, message, fields);

        public static void Warn(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Warning, null, message, fields);

        public static void Warn(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Warning, exception, message, fields);

        public static void Error(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Error, null, message, fields);

        public static void Error(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Error, exception, message, fields);

        public static void Fatal(this ILogger logger, string message, params (string Key, object? Value)[] fields) =>
            Write(logger, LogLevel.Critical, null, message, fields);

        public static void Fatal(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields) =>
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
