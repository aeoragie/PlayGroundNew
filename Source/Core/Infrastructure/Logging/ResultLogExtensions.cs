using NLog;
using PlayGround.Shared.Result;

namespace PlayGround.Infrastructure.Logging
{
    /// <summary>
    /// Result 수신 지점 로깅 헬퍼. DetailCode가 스스로 레벨을 결정한다
    /// (시스템 오류 → Error/Fatal, 비즈니스 오류 → Warn, 사용자 입력 오류·성공 → Info).
    /// 실패 Result를 받은 로직은 반드시 LogWith(또는 명시적 로깅)를 호출한다.
    /// </summary>
    public static class ResultLogExtensions
    {
        public static Result<T> LogWith<T>(this Result<T> result, ILogger logger, string operation)
        {
            Write(logger, result.ResultData, result.IsSuccess, operation);
            return result;
        }

        public static Result LogWith(this Result result, ILogger logger, string operation)
        {
            Write(logger, result.ResultData, result.IsSuccess, operation);
            return result;
        }

        private static void Write(ILogger logger, ResultInfo resultData, bool isSuccess, string operation)
        {
            var level = ToNLogLevel(resultData.DetailCode);
            if (!logger.IsEnabled(level))
            {
                return;
            }

            var fields = string.IsNullOrEmpty(resultData.Details)
                ? new (string, object?)[] { ("Operation", operation), ("Code", resultData.DetailCode.Name), ("Message", resultData.Message) }
                : new (string, object?)[] { ("Operation", operation), ("Code", resultData.DetailCode.Name), ("Message", resultData.Message), ("Details", resultData.Details) };

            var status = isSuccess ? "Operation completed" : "Operation failed";
            var logEvent = new LogEventInfo(level, logger.Name, KvLogExtensions.BuildMessage(status, fields));

            foreach (var (key, value) in fields)
            {
                logEvent.Properties[key] = value;
            }

            logger.Log(logEvent);
        }

        private static LogLevel ToNLogLevel(DetailCode code)
        {
            return code.GetLogLevel() switch
            {
                "Fatal" => LogLevel.Fatal,
                "Error" => LogLevel.Error,
                "Warning" => LogLevel.Warn,
                _ => LogLevel.Info
            };
        }
    }
}
