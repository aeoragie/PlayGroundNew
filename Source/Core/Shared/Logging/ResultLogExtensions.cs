using Microsoft.Extensions.Logging;
using Results = PlayGround.Shared.Result;

namespace PlayGround.Shared.Logging
{
    public static class ResultLogExtensions
    {
        public static Results.Result<T> LogWith<T>(this Results.Result<T> result, ILogger logger, string operation,
            params (string Key, object? Value)[] fields)
        {
            Write(logger, result.ResultData, result.IsSuccess, operation, fields);
            return result;
        }

        public static Results.Result LogWith(this Results.Result result, ILogger logger, string operation,
            params (string Key, object? Value)[] fields)
        {
            Write(logger, result.ResultData, result.IsSuccess, operation, fields);
            return result;
        }

        private static void Write(ILogger logger, Results.ResultInfo resultData, bool isSuccess, string operation,
            (string Key, object? Value)[] extra)
        {
            LogLevel level = isSuccess ? LogLevel.Debug : ToLogLevel(resultData.DetailCode);
            if (logger is null || !logger.IsEnabled(level))
            {
                return;
            }

            var fields = new List<(string Key, object? Value)>(4 + extra.Length) { ("Operation", operation) };
            fields.AddRange(extra);
            fields.Add(("Code", resultData.DetailCode.Name));
            fields.Add(("Message", resultData.Message));
            if (!string.IsNullOrEmpty(resultData.Details))
            {
                fields.Add(("Details", resultData.Details));
            }

            string message = isSuccess ? "Operation completed" : "Operation failed";
            switch (level)
            {
                case LogLevel.Critical:
                    logger.Fatal(message, [.. fields]);
                    break;
                case LogLevel.Error:
                    logger.Error(message, [.. fields]);
                    break;
                case LogLevel.Warning:
                    logger.Warn(message, [.. fields]);
                    break;
                case LogLevel.Debug:
                    logger.Debug(message, [.. fields]);
                    break;
                default:
                    logger.Info(message, [.. fields]);
                    break;
            }
        }

        public static LogLevel ToLogLevel(this Results.DetailCode code) => code switch
        {
            Results.ErrorCode error => error.IsCritical ? LogLevel.Critical : LogLevel.Error,
            Results.WarningCode => LogLevel.Warning,
            _ => LogLevel.Information,
        };
    }
}
