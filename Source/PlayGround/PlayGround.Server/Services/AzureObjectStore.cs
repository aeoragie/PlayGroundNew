using System.Diagnostics;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// <see cref="IObjectStore"/>의 Azure Blob Storage 구현 (`Provider=Azure`) — **껍질만 있고 구현은 없다.**
    ///
    /// 자리를 미리 잡아 두는 이유는 <see cref="UploadStorageProvider"/>에 값이 있어서
    /// "어디를 채우면 되는가"가 코드에 드러나게 하려는 것뿐이다.
    /// 실제로 필요해질 때 아래를 한다:
    ///
    /// 1. `Azure.Storage.Blobs` 패키지 추가 (Directory.Packages.props에 버전 등록)
    /// 2. 생성자의 예외를 지우고 클라이언트를 만든다 — 자격 증명은 `DefaultAzureCredential`
    ///    (관리 ID). **연결 문자열이 필요하면 설정에 필드를 그때 추가한다** — 지금 미리 만들지 않는다
    /// 3. Put/Open을 구현한다. `BucketName`은 Azure에서 **컨테이너 이름**으로 읽는다.
    ///    **키 형태와 미존재=null 계약은 S3 구현과 같아야 한다**
    ///    (`RemoteUploadStorageTests`가 그 계약을 검증한다)
    /// </summary>
    public sealed class AzureObjectStore : IObjectStore
    {
        private const string NotImplemented =
            "UploadStorageProvider.Azure is not implemented yet. Use S3, or implement AzureObjectStore.";

        public AzureObjectStore(UploadStorageConfiguration settings)
        {
            Debug.Assert(settings != null, "settings is required");

            // 기동 시점에 실패시킨다 — 업로드가 처음 일어나는 순간에 알게 되면 이미 늦다
            throw new NotSupportedException(NotImplemented);
        }

        public Task PutAsync(
            string key, Stream content, string contentType, CancellationToken cancellation = default)
        {
            throw new NotSupportedException(NotImplemented);
        }

        public Task<UploadContent?> OpenAsync(string key, CancellationToken cancellation = default)
        {
            throw new NotSupportedException(NotImplemented);
        }
    }
}
