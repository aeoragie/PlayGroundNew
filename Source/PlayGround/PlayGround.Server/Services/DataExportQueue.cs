using System.Threading.Channels;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>데이터 내려받기 백그라운드 잡 큐 (Design.SettingsFlows ③) — 무제한 인메모리 채널.
    /// 요청 API가 Enqueue만 하고 즉시 반환한다. 워커(DataExportWorker)가 ReadAllAsync로 소비한다.
    /// 싱글톤 — 비내구성이라 재기동 시 Pending은 워커가 다시 넣어 재개한다.</summary>
    public sealed class DataExportQueue : IDataExportQueue
    {
        private readonly Channel<Guid> mChannel =
            Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

        public void Enqueue(Guid requestId)
        {
            // Unbounded라 항상 성공 — 실패해도 요청 저장에는 영향 없다(워커 재개가 백업)
            mChannel.Writer.TryWrite(requestId);
        }

        public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellation) =>
            mChannel.Reader.ReadAllAsync(cancellation);
    }
}
