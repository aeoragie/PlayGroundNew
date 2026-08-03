using System.Diagnostics;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// S3 업로드 읽기 — /uploads 프록시 서빙과 OG 렌더가 버킷 원본을 가져올 때 쓴다.
    /// 미존재(NoSuchKey)는 오류가 아니라 null — 지워진 이미지 URL이 남아 있는 정상 시나리오다.
    /// </summary>
    public sealed class S3UploadReader : IUploadReader
    {
        private readonly IAmazonS3 mS3;
        private readonly string mBucketName;

        public S3UploadReader(IAmazonS3 s3, UploadStorageConfiguration.S3Settings settings)
        {
            Debug.Assert(s3 != null, "s3 is required");
            Debug.Assert(settings != null, "settings is required");
            mS3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            mBucketName = settings?.BucketName ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<UploadContent?> OpenAsync(string relativeUrl, CancellationToken cancellation = default)
        {
            string? key = UploadPaths.KeyFromUrl(relativeUrl);
            if (key is null)
            {
                return null;
            }

            try
            {
                GetObjectResponse response = await mS3.GetObjectAsync(mBucketName, key, cancellation);
                string contentType = string.IsNullOrEmpty(response.Headers.ContentType)
                    ? UploadPaths.ContentTypeOf(key)
                    : response.Headers.ContentType;
                return new UploadContent(response.ResponseStream, contentType, response.ContentLength);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
