using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// S3 이미지 저장 (키 uploads/{category}/{yyyyMM}/{guid}{ext} — 로컬 어댑터와 동일 형태).
    /// 반환 URL도 "/uploads/..." 그대로다 — DB 저장값·클라이언트·검증 로직이 저장 위치를 모르게 유지한다.
    /// 버킷은 퍼블릭 차단 상태이고, 브라우저 서빙은 UploadsController(/uploads 프록시)가 맡는다.
    /// 자격 증명은 SDK 기본 체인(EC2 인스턴스 역할) — 서버에 액세스 키를 두지 않는다.
    /// </summary>
    public sealed class S3ImageStorageService : IImageStorage
    {
        private readonly IAmazonS3 mS3;
        private readonly string mBucketName;
        private readonly ILogger<S3ImageStorageService> mLogger;

        public S3ImageStorageService(
            IAmazonS3 s3, UploadStorageConfiguration.S3Settings settings, ILogger<S3ImageStorageService> logger)
        {
            Debug.Assert(s3 != null, "s3 is required");
            Debug.Assert(settings != null, "settings is required");
            Debug.Assert(logger != null, "logger is required");
            mS3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            mBucketName = settings?.BucketName ?? throw new ArgumentNullException(nameof(settings));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SaveAsync(
            string category, Stream content, string contentType, CancellationToken cancellation = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
            ArgumentNullException.ThrowIfNull(content);

            string key = UploadPaths.NewKey(category, UploadPaths.ImageExtensionOf(contentType));
            var request = new PutObjectRequest
            {
                BucketName = mBucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false, // 스트림 소유권은 호출자(컨트롤러의 await using)에 있다
            };
            await mS3.PutObjectAsync(request, cancellation);

            string url = "/" + key;
            mLogger.LogInformation("Image stored to S3. {{ Category:{Category}, Url:{Url} }}", category, url);
            return url;
        }
    }
}
