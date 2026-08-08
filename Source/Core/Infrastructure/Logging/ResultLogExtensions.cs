using NLog;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;

namespace PlayGround.Infrastructure.Logging
{
    /// <summary>
    /// Result 수신 지점 로깅 헬퍼 — 레벨 분류는 <c>ToLogLevel</c> 한 곳에 있고 여기서는 NLog 레벨로만 옮긴다.
    /// </summary>
    public static class ResultLogExtensions
    {
        public static Result<T> LogWith<T>(this Result<T> result, ILogger logger, string operation,
            params (string Key, object? Value)[] fields)
        {
            Write(logger, result.ResultData, result.IsSuccess, operation, fields);
            return result;
        }

        public static Result LogWith(this Result result, ILogger logger, string operation,
            params (string Key, object? Value)[] fields)
        {
            Write(logger, result.ResultData, result.IsSuccess, operation, fields);
            return result;
        }

        private static void Write(ILogger logger, ResultInfo resultData, bool isSuccess, string operation,
            (string Key, object? Value)[] extra)
        {
            var level = isSuccess ? LogLevel.Debug : ToNLogLevel(resultData.DetailCode);
            if (!logger.IsEnabled(level))
            {
                return;
            }

            var list = new List<(string Key, object? Value)>(4 + extra.Length) { ("Operation", operation) };
            list.AddRange(extra);
            list.Add(("Code", resultData.DetailCode.Name));
            list.Add(("Message", resultData.Message));
            if (!string.IsNullOrEmpty(resultData.Details))
            {
                list.Add(("Details", resultData.Details));
            }

            (string, object?)[] fields = [.. list];
            var status = isSuccess ? "Operation completed" : "Operation failed";
            var logEvent = new LogEventInfo(level, logger.Name, KeyValueLogExtensions.BuildMessage(status, fields));

            foreach (var (key, value) in fields)
            {
                logEvent.Properties[key] = value;
            }

            logger.Log(logEvent);
        }

        private static LogLevel ToNLogLevel(DetailCode code)
        {
            return code.ToLogLevel() switch
            {
                Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.Fatal,
                Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warn,
                _ => LogLevel.Info
            };
        }
    }
}
