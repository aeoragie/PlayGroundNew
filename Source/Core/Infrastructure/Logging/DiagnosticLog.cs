using NLog;
using System.Diagnostics;

namespace PlayGround.Infrastructure.Logging
{
    /// <summary>
    /// 진단 로그 스위치는 이 파일 하나다. 켜려면 Core.Infrastructure.csproj의 DefineConstants에
    /// LOG_DATABASE·LOG_REDIS·LOG_ACTOR를 추가하고 다시 빌드한다.
    /// [Conditional]이라 꺼진 카테고리는 호출과 인자 계산이 컴파일에서 통째로 사라진다.
    /// </summary>
    public static class DiagnosticLog
    {
        [Conditional("LOG_DATABASE")]
        public static void Database(ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            KeyValueLogExtensions.Debug(logger, message, fields);
        }

        [Conditional("LOG_REDIS")]
        public static void Redis(ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            KeyValueLogExtensions.Debug(logger, message, fields);
        }

        [Conditional("LOG_ACTOR")]
        public static void Actor(ILogger logger, string message, params (string Key, object? Value)[] fields)
        {
            KeyValueLogExtensions.Debug(logger, message, fields);
        }

        public static Stopwatch? DatabaseTimer()
        {
#if LOG_DATABASE
            return Stopwatch.StartNew();
#else
            return null;
#endif
        }
    }
}
