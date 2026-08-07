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
        public static Results.Result<T> LogWith<T>(this Results.Result<T> result, ILogger logger, string operation)
        {
            Write(logger, result.ResultData, result.IsSuccess, operation);
            return result;
        }

        public static Results.Result LogWith(this Results.Result result, ILogger logger, string operation)
        {
            Write(logger, result.ResultData, result.IsSuccess, operation);
            return result;
        }

        private static void Write(ILogger logger, Results.ResultInfo resultData, bool isSuccess, string operation)
        {
            LogLevel level = ToLogLevel(resultData.DetailCode);
            if (logger is null || !logger.IsEnabled(level))
            {
                return;
            }

            (string, object?)[] fields = string.IsNullOrEmpty(resultData.Details)
                ? [("Operation", operation), ("Code", resultData.DetailCode.Name), ("Message", resultData.Message)]
                : [("Operation", operation), ("Code", resultData.DetailCode.Name), ("Message", resultData.Message), ("Details", resultData.Details)];

            string message = isSuccess ? "Operation completed" : "Operation failed";
            switch (level)
            {
                case LogLevel.Critical: logger.FatalWith(message, fields); break;
                case LogLevel.Error: logger.ErrorWith(message, fields); break;
                case LogLevel.Warning: logger.WarnWith(message, fields); break;
                default: logger.InfoWith(message, fields); break;
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
