#if DEBUG
using System.Diagnostics;
using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Shared.Time;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// **디버그 빌드에만 존재한다** — 파일 전체가 `#if DEBUG`다.
    ///
    /// DB의 `SystemClockOffset`(디버그 배포본에만 있는 테이블)을 읽어 앱의 <see cref="DebugClock"/>에
    /// 같은 값을 채운다. **두 시계가 같이 움직여야** 시간 이동 테스트가 의미를 갖는다 —
    /// 앱만 옮기면 앱이 넘긴 시각이 DB의 "지금"보다 미래가 되고, DB만 옮기면 그 반대가 된다.
    ///
    /// 주기적으로 다시 읽는 이유는 테스트 중에 값을 바꾸기 때문이다. 재기동 없이 반영된다.
    ///
    /// 테이블이 없으면(운영본 스크립트만 적용한 로컬 DB) 조용히 0으로 둔다 — 개발을 막지 않는다.
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

                    // 시계가 밀린 상태를 모르고 디버깅하면 원인을 한참 헤맨다 — 바뀔 때마다 남긴다
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

    /// <summary>디버그 시계 오프셋 조회 — 테이블이 없으면 0.</summary>
    public sealed class DebugClockRepository : RepositoryBase
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public DebugClockRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<TimeSpan> ReadOffsetAsync(CancellationToken cancellation)
        {
            // 테이블이 없는 로컬(운영본만 적용)에서도 조용히 0 — OBJECT_ID로 먼저 확인한다
            const string Sql = @"
                IF OBJECT_ID('dbo.SystemClockOffset', 'U') IS NULL SELECT CAST(0 AS INT);
                ELSE SELECT TOP 1 [OffsetSeconds] FROM [dbo].[SystemClockOffset];";

            Result<int> seconds = await QuerySingleOrDefaultAsync<int>(Sql, cancellation: cancellation);
            return seconds.IsError ? TimeSpan.Zero : TimeSpan.FromSeconds(seconds.Value);
        }
    }
}
#endif
