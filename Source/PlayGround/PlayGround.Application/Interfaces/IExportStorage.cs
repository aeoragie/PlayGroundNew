namespace PlayGround.Application.Interfaces
{
    /// <summary>
    /// 데이터 내려받기 파일 저장 포트 (Design.SettingsFlows ③). **정적 서빙(UseStaticFiles) 밖의 비공개 경로**에
    /// 둔다 — 다운로드는 서명 URL 엔드포인트가 Ready·만료·횟수를 검증해야 하므로 URL로 우회되면 안 된다.
    /// 반환값은 공개 URL이 아니라 저장 키(StorageKey) — 다운로드 시 OpenReadAsync로 스트림을 연다.
    /// </summary>
    public interface IExportStorage
    {
        /// <summary>내보내기 파일을 저장하고 저장 키를 돌려준다. content는 완성된 zip 바이트 스트림.</summary>
        Task<string> SaveAsync(Guid requestId, Stream content, CancellationToken cancellation = default);

        /// <summary>저장 키로 파일 스트림을 연다 — 없으면 null.</summary>
        Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellation = default);
    }
}
