using NLog;
using PlayGround.Shared.Logging;

namespace PlayGround.Infrastructure.Logging
{
    public static class KeyValueLogExtensions
    {
        public static void Trace(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Trace, null, message, fields);
        }

        public static void Debug(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Debug, null, message, fields);
        }

        public static void Info(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Info, null, message, fields);
        }

        public static void Warn(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Warn, null, message, fields);
        }

        public static void Warn(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Warn, exception, message, fields);
        }

        public static void Error(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Error, null, message, fields);
        }

        public static void Error(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Error, exception, message, fields);
        }

        public static void Fatal(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Fatal, null, message, fields);
        }

        public static void Fatal(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Fatal, exception, message, fields);
        }

        private static void Write(ILogger logger, LogLevel level, Exception? exception, string message, (string Key, object? Value)[] fields)
        {
            if (!logger.IsEnabled(level))
            {
                return;
            }

            var logEvent = new LogEventInfo(level, logger.Name, BuildMessage(message, fields))
            {
                Exception = exception
            };

            foreach (var (key, value) in fields)
            {
                logEvent.Properties[key] = value;
            }

            logger.Log(logEvent);
        }

        internal static string BuildMessage(string message, (string Key, object? Value)[] fields) =>
            LogFieldFormatter.BuildRendered(message, fields);
    }
}
