using System.Text;
using NLog;

namespace PlayGround.Infrastructure.Logging
{
    /// <summary>
    /// { Key:Value, Key:Value } 포맷 로깅 헬퍼.
    /// 메시지는 사람이 읽는 문장 + KV 블록으로 남기고, 각 필드는 구조화 속성으로도 기록한다.
    /// 예) Logger.InfoKv("Player profile requested", ("PlayerId", 123)) → "Player profile requested. { PlayerId:123 }"
    /// </summary>
    public static class KvLogExtensions
    {
        public static void TraceKv(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Trace, null, message, fields);
        }

        public static void DebugKv(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Debug, null, message, fields);
        }

        public static void InfoKv(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Info, null, message, fields);
        }

        public static void WarnKv(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Warn, null, message, fields);
        }

        public static void ErrorKv(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Error, null, message, fields);
        }

        public static void ErrorKv(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Error, exception, message, fields);
        }

        public static void FatalKv(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Fatal, null, message, fields);
        }

        public static void FatalKv(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields)
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

        internal static string BuildMessage(string message, (string Key, object? Value)[] fields)
        {
            if (fields.Length == 0)
            {
                return message;
            }

            var builder = new StringBuilder(message);
            if (!message.EndsWith('.'))
            {
                builder.Append('.');
            }

            builder.Append(" { ");
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(fields[i].Key)
                    .Append(':')
                    .Append(fields[i].Value?.ToString() ?? "null");
            }
            builder.Append(" }");

            return builder.ToString();
        }
    }
}
