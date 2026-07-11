using NLog;
using NLog.Config;
using NLog.Targets;
using PlayGround.Infrastructure.Logging;
using PlayGround.Shared.Result;
using Xunit;

namespace PlayGround.Tests.Infrastructure
{
    public class LoggingTests
    {
        private static (Logger Logger, MemoryTarget Target) CreateLogger()
        {
            var target = new MemoryTarget("memory")
            {
                Layout = "${level}|${message}"
            };

            var config = new LoggingConfiguration();
            config.AddRuleForAllLevels(target);

            var factory = new LogFactory
            {
                Configuration = config
            };

            return (factory.GetLogger("test"), target);
        }

        [Fact]
        public void InfoWith_RendersKeyValueFormat()
        {
            var (logger, target) = CreateLogger();

            logger.InfoWith("Player profile requested", ("PlayerId", 123), ("TeamId", 45));

            Assert.Equal("Info|Player profile requested. { PlayerId:123, TeamId:45 }", target.Logs[0]);
        }

        [Fact]
        public void InfoWith_NoFields_RendersMessageOnly()
        {
            var (logger, target) = CreateLogger();

            logger.InfoWith("Application started.");

            Assert.Equal("Info|Application started.", target.Logs[0]);
        }

        [Fact]
        public void ErrorWith_NullValue_RendersNull()
        {
            var (logger, target) = CreateLogger();

            logger.ErrorWith("Lookup failed", ("Key", null));

            Assert.Equal("Error|Lookup failed. { Key:null }", target.Logs[0]);
        }

        [Fact]
        public void LogWith_SystemError_LogsAtErrorLevel()
        {
            var (logger, target) = CreateLogger();

            Result<int>.Error(ErrorCode.DatabaseTimeout).LogWith(logger, "GetPlayer");

            Assert.StartsWith("Error|Operation failed. { Operation:GetPlayer, Code:DatabaseTimeout", target.Logs[0]);
        }

        [Fact]
        public void LogWith_CriticalError_LogsAtFatalLevel()
        {
            var (logger, target) = CreateLogger();

            Result<int>.Error(ErrorCode.DatabaseError).LogWith(logger, "GetPlayer");

            Assert.StartsWith("Fatal|Operation failed.", target.Logs[0]);
        }

        [Fact]
        public void LogWith_UserError_LogsAtInfoLevel()
        {
            var (logger, target) = CreateLogger();

            Result<int>.Error(ErrorCode.NotFound).LogWith(logger, "GetPlayer");

            Assert.StartsWith("Info|Operation failed. { Operation:GetPlayer, Code:NotFound", target.Logs[0]);
        }

        [Fact]
        public void LogWith_Success_LogsAtInfoLevel()
        {
            var (logger, target) = CreateLogger();

            Result<int>.Success(1).LogWith(logger, "GetPlayer");

            Assert.StartsWith("Info|Operation completed. { Operation:GetPlayer, Code:Ok", target.Logs[0]);
        }

        [Fact]
        public void LogWith_ReturnsSameResult_ForChaining()
        {
            var (logger, _) = CreateLogger();

            var result = Result<int>.Success(7).LogWith(logger, "GetPlayer").Map(x => x * 2);

            Assert.Equal(14, result.Value);
        }
    }
}
