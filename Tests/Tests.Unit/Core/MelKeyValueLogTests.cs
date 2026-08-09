using FluentAssertions;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using Xunit;

namespace PlayGround.Tests.Unit.Core
{
    /// <summary>
    /// MEL 헬퍼가 실제로 렌더되는지 검증한다. NLog 쪽(LoggingTests)만 있던 시절, 장식용 중괄호가
    /// MEL 템플릿을 깨는 버그가 실서버 첫 실패 로그에서야 드러났다 — 렌더까지 돌려야 잡힌다.
    /// </summary>
    public class MelKeyValueLogTests
    {
        private sealed class CaptureLogger : ILogger
        {
            public List<string> Rendered { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Rendered.Add(formatter(state, exception));
            }
        }

        [Fact]
        public void Info_RendersKeyValueFormat()
        {
            var logger = new CaptureLogger();

            logger.Info("Team created", ("TeamId", 7), ("Slug", "fc-seoul"));

            logger.Rendered.Should().ContainSingle()
                .Which.Should().Be("Team created. { TeamId:7, Slug:fc-seoul }");
        }

        [Fact]
        public void LogWith_Failure_RendersWithoutThrowing()
        {
            var logger = new CaptureLogger();

            Result<int>.Error(ErrorCode.NotFound).LogWith(logger, "Execute", ("UserId", 3));

            logger.Rendered.Should().ContainSingle()
                .Which.Should().StartWith("Operation failed. { Operation:Execute, UserId:3, Code:NotFound");
        }
    }
}
