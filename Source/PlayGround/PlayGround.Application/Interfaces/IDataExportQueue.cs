namespace PlayGround.Application.Interfaces
{
    /// <summary>
    /// 데이터 내려받기 백그라운드 잡 큐 (Design.SettingsFlows ③). 요청 API는 접수만 하고 여기 넣은 뒤 즉시 반환한다
    /// (동기 생성 금지). 워커(BackgroundService)가 읽어 파일을 생성한다. 인메모리라 비내구성 —
    /// 재기동 시 Pending 요청을 워커가 다시 넣어 재개한다.
    /// </summary>
    public interface IDataExportQueue
    {
        void Enqueue(Guid requestId);

        /// <summary>큐를 소비한다 — 워커 전용. 취소될 때까지 도착하는 요청 Id를 순차로 돌려준다.</summary>
        IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellation);
    }
}
