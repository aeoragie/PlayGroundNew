using System.Diagnostics;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 오브젝트 저장소 첨부 문서 저장 — <see cref="RemoteImageStorageService"/>와 같은 구조지만
    /// **원본 확장자를 보존**한다(문서는 리사이즈·크롭이 없다).
    /// 허용 밖 확장자는 .bin으로 강등한다(로컬 어댑터와 동일한 최후 방어).
    /// </summary>
    public sealed class RemoteFileStorageService : IFileStorage
    {
        private readonly IObjectStore mStore;
        private readonly ILogger<RemoteFileStorageService> mLogger;

        public RemoteFileStorageService(IObjectStore store, ILogger<RemoteFileStorageService> logger)
        {
            Debug.Assert(store != null, "store is required");
            Debug.Assert(logger != null, "logger is required");
            mStore = store ?? throw new ArgumentNullException(nameof(store));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SaveAsync(
            string category, Stream content, string originalFileName, CancellationToken cancellation = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
            ArgumentNullException.ThrowIfNull(content);

            string key = UploadPaths.NewKey(category, UploadPaths.SafeDocumentExtension(originalFileName));
            await mStore.PutAsync(key, content, UploadPaths.ContentTypeOf(key), cancellation);

            string url = "/" + key;
            mLogger.LogInformation("File stored to object store. {{ Category:{Category}, Url:{Url} }}", category, url);
            return url;
        }
    }
}
