using PlayGround.Application.Export.Commands;
using PlayGround.Application.Interfaces;
using PlayGround.Shared.Result;

namespace PlayGround.Server.Services
{
    /// <summary>데이터 내려받기 백그라운드 워커 (Design.SettingsFlows ③). 큐를 소비해 파일을 생성한다.
    /// 기동 시 남아 있는 Pending 요청을 큐에 다시 넣어 재개한다(인메모리 큐는 비내구성 — 재기동 시 유실 방어).
    /// 요청마다 DI 스코프를 열어 DataExportCommand를 리졸브(스코프드 리포지토리 사용).</summary>
    public sealed class DataExportWorker : BackgroundService
    {
        private readonly IServiceProvider mServiceProvider;
        private readonly IDataExportQueue mQueue;
        private readonly ILogger<DataExportWorker> mLogger;

        public DataExportWorker(IServiceProvider serviceProvider, IDataExportQueue queue, ILogger<DataExportWorker> logger)
        {
            mServiceProvider = serviceProvider;
            mQueue = queue;
            mLogger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ResumePendingAsync(stoppingToken);

            try
            {
                await foreach (Guid requestId in mQueue.ReadAllAsync(stoppingToken))
                {
                    await ProcessAsync(requestId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ResumePendingAsync(CancellationToken cancellation)
        {
            try
            {
                using IServiceScope scope = mServiceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ISoccerDataExportRepository>();
                Result<List<Guid>> pending = await repository.GetPendingIdsAsync(cancellation);
                if (!pending.IsError && pending.Value.Count > 0)
                {
                    mLogger.LogInformation("Resuming {Count} pending data export(s).", pending.Value.Count);
                    foreach (Guid id in pending.Value)
                    {
                        mQueue.Enqueue(id);
                    }
                }
            }
            catch (Exception ex)
            {
                mLogger.LogWarning(ex, "Failed to resume pending data exports.");
            }
        }

        private async Task ProcessAsync(Guid requestId, CancellationToken cancellation)
        {
            try
            {
                using IServiceScope scope = mServiceProvider.CreateScope();
                var command = scope.ServiceProvider.GetRequiredService<DataExportCommand>();
                await command.GenerateAsync(requestId, cancellation);
            }
            catch (Exception ex)
            {
                // GenerateAsync가 자체적으로 Failed 처리하지만, 스코프 생성 실패 등은 여기서 삼킨다
                mLogger.LogError(ex, "Data export processing threw. {{ RequestId:{RequestId} }}", requestId);
            }
        }
    }
}
