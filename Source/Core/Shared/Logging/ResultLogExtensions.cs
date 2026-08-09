using Microsoft.Extensions.Logging;
using PlayGround.Shared.Result;
using Results = PlayGround.Shared.Result;

namespace PlayGround.Shared.Logging
{
    /// <summary>
    /// Result 수신 지점 로깅. 아래층이 Error를 남기지 않으므로 유즈케이스가 이걸 빼먹으면 오류가 로그에 안 남는다.
    /// </summary>
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
            // 성공까지 Info로 남기면 조회 한 번에 한 줄씩 쌓인다. 의미 있는 성공은 유즈케이스가 직접 남긴다.
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
                case LogLevel.Critical: logger.Fatal(message, [.. fields]); break;
                case LogLevel.Error: logger.Error(message, [.. fields]); break;
                case LogLevel.Warning: logger.Warn(message, [.. fields]); break;
                case LogLevel.Debug: logger.Debug(message, [.. fields]); break;
                default: logger.Info(message, [.. fields]); break;
            }
        }

        /// <summary>코드 종류가 곧 레벨이다. 프로세스를 못 버티는 오류만 Critical로 올린다.</summary>
        public static LogLevel ToLogLevel(this Results.DetailCode code) => code switch
        {
            Results.ErrorCode error => error.IsCritical ? LogLevel.Critical : LogLevel.Error,
            Results.WarningCode => LogLevel.Warning,
            _ => LogLevel.Information,
        };
    }
}
