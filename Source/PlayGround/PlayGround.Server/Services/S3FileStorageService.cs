using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// S3 첨부 문서 저장 — S3ImageStorageService와 같은 구조지만 **원본 확장자를 보존**한다
    /// (문서는 리사이즈·크롭이 없다). 허용 밖 확장자는 .bin으로 강등(로컬 어댑터와 동일한 최후 방어).
    /// </summary>
    public sealed class S3FileStorageService : IFileStorage
    {
        private readonly IAmazonS3 mS3;
        private readonly string mBucketName;
        private readonly ILogger<S3FileStorageService> mLogger;

        public S3FileStorageService(
            IAmazonS3 s3, UploadStorageConfiguration.S3Settings settings, ILogger<S3FileStorageService> logger)
        {
            Debug.Assert(s3 != null, "s3 is required");
            Debug.Assert(settings != null, "settings is required");
            Debug.Assert(logger != null, "logger is required");
            mS3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            mBucketName = settings?.BucketName ?? throw new ArgumentNullException(nameof(settings));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SaveAsync(
            string category, Stream content, string originalFileName, CancellationToken cancellation = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
            ArgumentNullException.ThrowIfNull(content);

            string key = UploadPaths.NewKey(category, UploadPaths.SafeDocumentExtension(originalFileName));
            var request = new PutObjectRequest
            {
                BucketName = mBucketName,
                Key = key,
                InputStream = content,
                ContentType = UploadPaths.ContentTypeOf(key),
                AutoCloseStream = false, // 스트림 소유권은 호출자에 있다
            };
            await mS3.PutObjectAsync(request, cancellation);

            string url = "/" + key;
            mLogger.LogInformation("File stored to S3. {{ Category:{Category}, Url:{Url} }}", category, url);
            return url;
        }
    }
}
