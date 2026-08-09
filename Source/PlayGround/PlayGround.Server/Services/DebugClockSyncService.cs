#if DEBUG
using Microsoft.Extensions.Options;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using System.Diagnostics;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// DB의 <c>SystemClockOffset</c>을 주기적으로 읽어 앱의 <see cref="DebugClock"/>에 같은 값을 채운다.
    /// 두 시계가 같이 움직여야 시간 이동 테스트가 의미를 갖는다. 파일 전체가 <c>#if DEBUG</c>다.
    /// </summary>
    public sealed class DebugClockSyncService : BackgroundService
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

        private readonly DebugClockRepository mRepository;
        private readonly ILogger<DebugClockSyncService> mLogger;

        public DebugClockSyncService(DebugClockRepository repository, ILogger<DebugClockSyncService> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            Debug.Assert(logger != null, "logger is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan applied = TimeSpan.Zero;

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan offset = await mRepository.ReadOffsetAsync(stoppingToken);
                if (offset != applied)
                {
                    DebugClock.Shift(offset);
                    applied = offset;

                    mLogger.LogWarning(
                        "Debug clock shifted. {{ OffsetSeconds:{OffsetSeconds} }}", (long)offset.TotalSeconds);
                }

                try
                {
                    await Task.Delay(RefreshInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    public sealed class DebugClockRepository : RepositoryBase
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public DebugClockRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<TimeSpan> ReadOffsetAsync(CancellationToken cancellation)
        {
            // 테이블이 없는 로컬(운영본 스크립트만 적용)에서도 조용히 0으로 둔다
            const string Sql = @"
                IF OBJECT_ID('dbo.SystemClockOffset', 'U') IS NULL SELECT CAST(0 AS INT);
                ELSE SELECT TOP 1 [OffsetSeconds] FROM [dbo].[SystemClockOffset];";

            Result<int> seconds = await QuerySingleOrDefaultAsync<int>(Sql, cancellation: cancellation);
            return seconds.IsError ? TimeSpan.Zero : TimeSpan.FromSeconds(seconds.Value);
        }
    }
}
#endif
