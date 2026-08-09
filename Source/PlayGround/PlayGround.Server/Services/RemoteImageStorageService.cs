using PlayGround.Application.Interfaces;
using System.Diagnostics;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 오브젝트 저장소 이미지 저장 (키 uploads/{category}/{yyyyMM}/{guid}{ext} — 로컬 어댑터와 동일 형태).
    /// 반환 URL도 "/uploads/..." 그대로다 — DB 저장값·클라이언트·검증 로직이 **저장 위치를 모르게** 유지한다.
    ///
    /// 저장소가 어디인지는 <see cref="IObjectStore"/> 뒤에 숨는다(현재 AWS S3).
    /// 브라우저 서빙은 UploadsController(`/uploads` 프록시)가 맡는다.
    /// </summary>
    public sealed class RemoteImageStorageService : IImageStorage
    {
        private readonly IObjectStore mStore;
        private readonly ILogger<RemoteImageStorageService> mLogger;

        public RemoteImageStorageService(IObjectStore store, ILogger<RemoteImageStorageService> logger)
        {
            Debug.Assert(store != null, "store is required");
            Debug.Assert(logger != null, "logger is required");
            mStore = store ?? throw new ArgumentNullException(nameof(store));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SaveAsync(
            string category, Stream content, string contentType, CancellationToken cancellation = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
            ArgumentNullException.ThrowIfNull(content);

            string key = UploadPaths.NewKey(category, UploadPaths.ImageExtensionOf(contentType));
            await mStore.PutAsync(key, content, contentType, cancellation);

            string url = "/" + key;
            mLogger.LogInformation("Image stored to object store. {{ Category:{Category}, Url:{Url} }}", category, url);
            return url;
        }
    }
}
