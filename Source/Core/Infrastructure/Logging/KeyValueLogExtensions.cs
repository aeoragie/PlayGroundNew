using System.Text;
using NLog;

namespace PlayGround.Infrastructure.Logging
{
    /// <summary>
    /// { Key:Value, Key:Value } 포맷 로깅 헬퍼.
    /// 메시지는 사람이 읽는 문장 + KeyValue 블록으로 남기고, 각 필드는 구조화 속성으로도 기록한다.
    /// 예) Logger.InfoWith("Player profile requested", ("PlayerId", 123)) → "Player profile requested. { PlayerId:123 }"
    /// </summary>
    public static class KeyValueLogExtensions
    {
        public static void TraceWith(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Trace, null, message, fields);
        }

        public static void DebugWith(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Debug, null, message, fields);
        }

        public static void InfoWith(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Info, null, message, fields);
        }

        public static void WarnWith(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Warn, null, message, fields);
        }

        public static void ErrorWith(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Error, null, message, fields);
        }

        public static void ErrorWith(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Error, exception, message, fields);
        }

        public static void FatalWith(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            Write(logger, LogLevel.Fatal, null, message, fields);
        }

        public static void FatalWith(this ILogger logger, Exception exception, string message, params (string Key, object? Value)[] fields)
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
